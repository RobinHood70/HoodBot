namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Design;
using RobinHood70.HoodBot.Jobs.Design;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

internal sealed class EsoSets : EditJob
{
	#region Private Constants
	private const string Query =
		"SELECT id, setName, setBonusDesc1, setBonusDesc2, setBonusDesc3, setBonusDesc4, setBonusDesc5, setBonusDesc6, setBonusDesc7, setBonusDesc8, setBonusDesc9, setBonusDesc10, setBonusDesc11, setBonusDesc12\n" +
		"FROM setSummary";
	#endregion

	#region Static Fields
	private static readonly Dictionary<string, string> TitleOverrides = new(StringComparer.Ordinal)
	{
		// Title Overrides should only be necessary when creating new disambiguated "(set)" pages or when pages don't conform to the base/base (set) style. While this could be done programatically, it's probably best not to, so that a human has verified that the page really should be created and that the existing page isn't malformed or something.
		["Anthelmir's Construct"] = "Anthelmir's Construct (set)",
		["Camonna Tong"] = "Camonna Tong (set)",
		["Dro'Zakar's Claws"] = "Dro'zakar's Claws",
		["Knight-errant's Mail"] = "Knight-Errant's Mail",
		["Roksa the Warped"] = "Roksa the Warped (set)",
		["The Blind"] = "The Blind (set)",
	};

	private static string? blankText;
	#endregion

	#region Fields
	private readonly Dictionary<Title, SetData> sets = [];
	#endregion

	#region Constructors
	[JobInfo("Sets", "ESO Update")]
	public EsoSets(JobManager jobManager, bool hideDiffs)
		: base(jobManager)
	{
		jobManager.ShowDiffs = !hideDiffs;
		if (this.Results is PageResultHandler pageResults)
		{
			var title = pageResults.Title;
			pageResults.Title = TitleFactory.FromValidated(title.Namespace, title.PageName + "/ESO Sets");
			pageResults.SaveAsBot = false;
		}

		// TODO: Rewrite Mod Header handling to be more intelligent.
		this.StatusWriteLine("DON'T FORGET TO UPDATE MOD HEADER!");
	}
	#endregion

	#region Public Override Properties
	public override string LogName => "Update ESO Sets";
	#endregion

	#region Protected Override Methods

	// Needs to be after update, since update modifies item's IsNonTrivial property.
	protected override void AfterLoadPages() => this.GenerateReport();

	protected override void BeforeLoadPages()
	{
		UespReplacer.Initialize(this);
		this.StatusWriteLine("Fetching data");

		PageCollection catPages = new(this.Site, PageModules.Info, true);
		catPages.GetCategoryMembers("Online-Sets", CategoryMemberTypes.Page, false);

		var allSets = GetSetData();
		var unresolved = this.UpdateSetPages(catPages, allSets);
		foreach (var setName in unresolved)
		{
			this.Warn($"A page for {setName} could not be determined. Please check this and add the title to {nameof(TitleOverrides)}.");
		}

		blankText = this.Site.LoadUserSubPageText("Blank Set");
		if (blankText is not null)
		{
			var index = blankText.IndexOf("-->", StringComparison.Ordinal);
			if (index != -1)
			{
				blankText = blankText[(index + 3)..].TrimStart();
			}
		}
	}

	protected override string GetEditSummary(Page page) => this.LogName;

	protected override bool GetIsMinorEdit(Page page) => false;

	protected override void JobCompleted()
	{
		UespReplacer.ShowUnreplaced();
		base.JobCompleted();
	}

	protected override void LoadPages()
	{
		TitleCollection titles = new(this.Site);
		foreach (var (title, _) in this.sets)
		{
			titles.Add(title);
		}

		this.Pages.GetTitles(titles);
	}

	protected override void Main()
	{
		this.SavePages();
		var version = EsoLog.LatestDBUpdate(false);
		EsoSpace.SetBotUpdateVersion(this, "botitemset", version);
	}

	protected override void PageMissing(Page page) => page.Text = blankText?
		.Replace("«Mod Header»", "{{Mod Header|Gold Road}}", StringComparison.Ordinal)
		.Replace("«Set»", this.sets[page.Title].Name, StringComparison.Ordinal);

	protected override void PageLoaded(Page page)
	{
		var setData = this.sets[page.Title];
		SiteParser oldPage = new(page, InclusionType.Transcluded, false);
		if (oldPage.Count < 2 || !(
				oldPage[0] is IIgnoreNode firstNode &&
				firstNode.Value.EndsWith("<onlyinclude>", StringComparison.Ordinal) &&
				oldPage[^1] is IIgnoreNode lastNode &&
				lastNode.Value.StartsWith("</onlyinclude>", StringComparison.Ordinal)))
		{
			this.Warn($"Delimiters not found on page {page.Title.FullPageName()}");
			return;
		}

		var oldText = oldPage[1..^2].ToRaw().Trim();
		var oldTextMatch = Regex.Match(oldText, @"^'''\d", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);
		var preText = oldText[0..oldTextMatch.Index];

		TitleCollection usedList = new(this.Site);
		StringBuilder sb = new();
		sb
			.Append('\n')
			.Append(preText);
		foreach (var (itemCount, text) in setData.BonusDescriptions)
		{
			sb
				.Append("'''")
				.Append(itemCount)
				.Append("''': ")
				.Append(text)
				.Append("<br>\n");
		}

		sb.Remove(sb.Length - 5, 4);
		SiteParser parser = new(page, sb.ToString());
		UespReplacer.ReplaceGlobal(parser);
		UespReplacer.ReplaceEsoLinks(parser);
		UespReplacer.ReplaceFirstLink(parser, usedList);

		// Now that we're done parsing, re-add the IgnoreNodes.
		parser.Insert(0, firstNode);
		parser.Add(lastNode);
		var replacer = new UespReplacer(this.Site, oldPage, parser);
		foreach (var warning in replacer.Compare(parser.Title.FullPageName()))
		{
			this.Warn(warning);
		}

		setData.IsNonTrivial = replacer.IsNonTrivialChange();
		parser.UpdatePage();
	}
	#endregion

	#region Private Static Methods
	private static List<SetData> GetSetData()
	{
		var retval = new List<SetData>();
		foreach (var item in Database.RunQuery(EsoLog.Connection, Query, row => new SetData(row)))
		{
			retval.Add(item);
		}

		return retval;
	}
	#endregion

	#region Private Methods
	private void GenerateReport()
	{
		var nonTrivial = new SortedDictionary<Title, string>(TitleComparer.Instance);
		var trivial = new SortedDictionary<Title, string>(TitleComparer.Instance);
		StringBuilder sb = new();
		foreach (var (title, set) in this.sets)
		{
			sb.Clear();
			if (set.IsNonTrivial)
			{
				sb
					.Append("* {{Pl|")
					.Append(title.FullPageName())
					.Append('|')
					.Append(set.Name)
					.Append("|diff=cur}}");
				nonTrivial.Add(title, sb.ToString());
			}
			else
			{
				trivial.Add(title, SiteLink.ToText(title, LinkFormat.LabelName));
			}
		}

		if (nonTrivial.Count > 0)
		{
			this.WriteLine("== Sets With Non-Trivial Updates ==");
			foreach (var (_, text) in nonTrivial)
			{
				this.WriteLine(text);
			}
		}

		if (trivial.Count > 0)
		{
			if (nonTrivial.Count > 0)
			{
				this.WriteLine();
				this.WriteLine();
			}

			this.WriteLine("== Sets With Trivial Updates==");
			this.WriteLine(string.Join(", ", trivial.Values));
		}
	}

	private SortedSet<string> ResolveDisambiguations(Dictionary<Title, SetData> notFound)
	{
		var unresolved = new SortedSet<string>(StringComparer.Ordinal);
		var titles = new TitleCollection(this.Site, notFound.Keys);
		var pages = titles.Load(PageModules.Info | PageModules.Links | PageModules.Properties, true);
		foreach (var page in pages)
		{
			var title = page.Title;
			var set = notFound[title];
			if (page.IsDisambiguation == true && unresolved.Contains(set.Name))
			{
				var resolved = false;
				foreach (var link in page.Links)
				{
					if (link.PageName.Contains(" (set)", StringComparison.OrdinalIgnoreCase))
					{
						this.sets.Add(title, set);
						resolved = true;
						break;
					}
				}

				if (!resolved)
				{
					unresolved.Add(set.Name);
				}
			}
			else if (page.Exists && title.PageName.EndsWith(" (set)", StringComparison.Ordinal))
			{
				unresolved.Add(set.Name);
			}
			else
			{
				// Unless it's a (set) page, add it to the list. If missing, it'll get created.
				this.sets.Add(title, set);
			}
		}

		return unresolved;
	}

	private SortedSet<string> UpdateSetPages(PageCollection setMembers, List<SetData> sets)
	{
		var notFound = new Dictionary<Title, SetData>();
		foreach (var set in sets)
		{
			if (TitleOverrides.TryGetValue(set.Name, out var overrideName))
			{
				var title = TitleFactory.FromValidated(this.Site[UespNamespaces.Online], overrideName);
				this.sets.Add(title, set);
				continue;
			}

			Page? foundPage = null;
			var checkSets = new[] { set.Name + " (set)", set.Name };
			foreach (var setName in checkSets)
			{
				var checkTitle = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], setName);
				if (setMembers.TryGetValue(checkTitle, out foundPage))
				{
					break;
				}

				foreach (var setMember in setMembers)
				{
					if (string.Compare(setMember.Title.PageName, setName, true, this.Site.Culture) == 0 &&
						string.Compare(setMember.Title.PageName, setName, false, this.Site.Culture) != 0)
					{
						foundPage = setMember;
						this.Warn($"Substituted {setMember.Title.PageName} for {setName}. It's safer to override this in {nameof(TitleOverrides)}.");
						break;
					}
				}
			}

			if (foundPage is not null)
			{
				this.sets.Add(foundPage.Title, set);
			}
			else
			{
				var title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], set.Name);
				notFound.Add(title, set);
			}
		}

		return this.ResolveDisambiguations(notFound);
	}
	#endregion

	#region Private Classes
	private sealed class SetData
	{
		#region Constructors
		public SetData(IDataRecord row)
		{
			ArgumentNullException.ThrowIfNull(row);
			this.Name = EsoLog.ConvertEncoding((string)row["setName"]);
			for (var i = 1; i <= 12; i++)
			{
				var bonusDesc = EsoLog.ConvertEncoding((string)row[$"setBonusDesc{i}"]);
				if (!string.IsNullOrEmpty(bonusDesc))
				{
					var bonusSplit = bonusDesc.Split(") ", 2, StringSplitOptions.None);
					var items = bonusSplit[0];
					if (bonusSplit.Length != 2 || items[0] != '(')
					{
						throw new InvalidOperationException($"Set bonus for {this.Name} is improperly formatted: {bonusDesc}");
					}

					items = items[1..];
					var text = bonusSplit[1].Trim();
					text = RegexLibrary.PruneExcessWhitespace(text);
					if (text.StartsWith(this.Name, StringComparison.Ordinal))
					{
						text = text[this.Name.Length..].TrimStart();
					}

					this.BonusDescriptions.Add((items, text));
				}
			}
		}
		#endregion

		#region Public Properties

		public List<(string ItemCount, string Text)> BonusDescriptions { get; } = [];

		public bool IsNonTrivial { get; set; }

		public string Name { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
	#endregion
}