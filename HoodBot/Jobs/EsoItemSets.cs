namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoItemSets : EditJob
	{
		#region Private Constants
		private const string Query =
			"SELECT id, setName, setBonusDesc1, setBonusDesc2, setBonusDesc3, setBonusDesc4, setBonusDesc5, setBonusDesc6, setBonusDesc7, setBonusDesc8, setBonusDesc9, setBonusDesc10, setBonusDesc11, setBonusDesc12\n" +
			"FROM setSummary\n";
		#endregion

		#region Static Fields
		private static readonly Dictionary<string, string> TitleOverrides = new(StringComparer.Ordinal)
		{
			// Title Overrides should only be necessary when creating new disambiguated "(set)" pages or when pages don't conform to the base/base (set) style. While this could be done programatically, it's probably best not to, so that a human has verified that the page really should be created and that the existing page isn't malformed or something.
			["Dro'Zakar's Claws"] = "Dro'zakar's Claws",
			["Roksa the Warped"] = "Roksa the Warped (set)"
		};
		#endregion

		#region Fields
		private readonly Dictionary<Title, SetData> sets = new(SimpleTitleComparer.Instance);
		#endregion

		#region Constructors
		[JobInfo("Item Sets", "ESO Update")]
		public EsoItemSets(JobManager jobManager)
			: base(jobManager)
		{
			// jobManager.ShowDiffs = false;
			this.MinorEdit = false;
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Update ESO Item Sets";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => this.LogName;
		#endregion

		#region Protected Override Methods

		// Needs to be after update, since update modifies item's IsNonTrivial property.
		protected override void AfterLoadPages() => this.GenerateReport();

		protected override void BeforeLoadPages()
		{
			EsoReplacer.Initialize(this);
			this.StatusWriteLine("Fetching data");
			var allSets = GetSetData();
			PageCollection catPages = new(this.Site, PageModules.Info, true);
			catPages.GetCategoryMembers("Online-Sets", CategoryMemberTypes.Page, false);
			var unresolved = this.UpdateSetPages(catPages, allSets);
			foreach (var setName in unresolved)
			{
				this.Warn($"A page for {setName} could not be determined. Please check this and add the title to {nameof(TitleOverrides)}.");
			}
		}

		protected override void JobCompleted()
		{
			EsoReplacer.ShowUnreplaced();
			base.JobCompleted();
		}

		protected override void LoadPages()
		{
			TitleCollection titles = new(this.Site);
			foreach (var (page, _) in this.sets)
			{
				titles.Add(page);
			}

			this.Pages.GetTitles(titles);
		}

		protected override void Main()
		{
			this.SavePages();
			EsoSpace.SetBotUpdateVersion(this, "itemset");
		}

		protected override void PageMissing(Page page)
		{
			var set = this.sets[page];
			StringBuilder sb = new();
			sb
				.Append("{{Trail|Sets}}{{Online Update}}{{Minimal}}\n")
				.Append("'''")
				.Append(set.Name)
				.Append("''' is a {{huh}}-rank [[Online:Sets|item set]] found in {{huh}}.\n\n")
				.Append("Set pieces are {{huh}} style in {{huh}}.\n\n")
				.Append("===Bonuses===\n")
				.Append("<onlyinclude>\n")
				.Append("</onlyinclude>\n")
				.Append("===Pieces===\n")
				.Append("The set consists of {{huh}}. For detailed weapon stats and set values, see individual item pages by clicking on the links below.\n")
				.Append("<!--\n")
				.Append("* {{Item Link|id=|}}\n")
				.Append("* {{Item Link|id=|}}\n")
				.Append("-->\n")
				.Append("===Notes===\n\n")
				.Append("{{Online Sets}}\n")
				.Append("{{ESO Sets With|subtype=|source=}}\n")
				.Append("{{Stub|Item Set}}");
			page.Text = sb.ToString();
		}

		protected override void PageLoaded(Page page)
		{
			var setData = this.sets[page];
			ContextualParser oldPage = new(page, InclusionType.Transcluded, false);
			if (oldPage.Count < 2 || !(
					oldPage[0] is IIgnoreNode firstNode &&
					oldPage[^1] is IIgnoreNode lastNode))
			{
				this.Warn($"Delimiters not found on page {page.FullPageName}\n");
				return;
			}

			TitleCollection usedList = new(this.Site);
			StringBuilder sb = new();
			sb.Append('\n');
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
			ContextualParser parser = new(page, sb.ToString());
			EsoReplacer.ReplaceGlobal(parser);
			EsoReplacer.ReplaceEsoLinks(this.Site, parser);
			EsoReplacer.ReplaceFirstLink(parser, usedList);

			// Now that we're done parsing, re-add the IgnoreNodes.
			parser.Insert(0, firstNode);
			parser.Add(lastNode);

			EsoReplacer replacer = new(this.Site);
			var newLinks = replacer.CheckNewLinks(oldPage, parser);
			if (newLinks.Count > 0)
			{
				this.Warn(EsoReplacer.ConstructWarning(oldPage, parser, newLinks, "links"));
			}

			var newTemplates = replacer.CheckNewTemplates(oldPage, parser);
			if (newTemplates.Count > 0)
			{
				this.Warn(EsoReplacer.ConstructWarning(oldPage, parser, newTemplates, "templates"));
			}

			setData.IsNonTrivial = replacer.IsNonTrivialChange(oldPage, parser);
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
			var sorted = new SortedDictionary<Title, string>(SimpleTitleComparer.Instance);
			foreach (var (page, set) in this.sets)
			{
				StringBuilder sb = new();
				if (set.IsNonTrivial)
				{
					sb
						.Append("* {{Pl|")
						.Append(page.FullPageName)
						.Append('|')
						.Append(set.Name)
						.Append("|diff=cur}}");
					sorted.Add(page, sb.ToString());
				}
			}

			if (sorted.Count > 0)
			{
				this.WriteLine("== Item Sets With Non-Trivial Updates ==");
				foreach (var (_, text) in sorted)
				{
					this.WriteLine(text);
				}
			}
		}

		private SortedSet<string> ResolveDisambiguations(Dictionary<Title, SetData> notFound)
		{
			var unresolved = new SortedSet<string>(StringComparer.Ordinal);
			var titles = new TitleCollection(this.Site, notFound.Keys);
			var pages = titles.Load(PageModules.Info | PageModules.Links | PageModules.Properties, true);
			foreach (var page in pages)
			{
				var set = notFound[page];
				if (page.IsDisambiguation == true && unresolved.Contains(set.Name))
				{
					var resolved = false;
					foreach (var link in page.Links)
					{
						if (link.PageName.Contains(" (set)", StringComparison.OrdinalIgnoreCase))
						{
							this.sets.Add(page, set);
							resolved = true;
							break;
						}
					}

					if (!resolved)
					{
						unresolved.Add(set.Name);
					}
				}
				else if (page.Exists && page.PageName.EndsWith(" (set)", StringComparison.Ordinal))
				{
					unresolved.Add(set.Name);
				}
				else
				{
					// Unless it's a (set) page, add it to the list. If missing, it'll get created.
					this.sets.Add(page, set);
				}
			}

			return unresolved;
		}

		private SortedSet<string> UpdateSetPages(PageCollection setMembers, List<SetData> sets)
		{
			var notFound = new Dictionary<Title, SetData>(SimpleTitleComparer.Instance);
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
						if (string.Compare(setMember.PageName, setName, true, this.Site.Culture) == 0 &&
							string.Compare(setMember.PageName, setName, false, this.Site.Culture) != 0)
						{
							foundPage = setMember;
							this.Warn($"Substituted {setMember.PageName} for {setName}. It's safer to override this in {nameof(TitleOverrides)}.");
							break;
						}
					}

					if (foundPage is not null)
					{
						this.sets.Add(foundPage, set);
						break;
					}
				}

				if (foundPage is null)
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
				this.Name = (string)row.NotNull()["setName"];
				for (var i = 1; i <= 12; i++)
				{
					var bonusDesc = (string)row[$"setBonusDesc{i}"];
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
						text = RegexLibrary.WhitespaceToSpace(text);
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

			public List<(string ItemCount, string Text)> BonusDescriptions { get; } = new();

			public bool IsNonTrivial { get; set; }

			public string Name { get; }
			#endregion

			#region Public Override Methods
			public override string ToString() => this.Name;
			#endregion
		}
		#endregion
	}
}
