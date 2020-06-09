namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WikiCommon;

	internal class EsoItemSets : EditJob
	{
		#region Static Fields
		private static readonly HashSet<int> BadRows = new HashSet<int> { 1226, 1240 };
		private static readonly Regex OnlineUpdateRegex = Template.Find("Online Update");
		private static readonly Regex SetBonusRegex = new Regex(@"(\([1-6] items?\))");
		private static readonly Uri SetSummaryPage = new Uri("http://esolog.uesp.net/viewlog.php?record=setSummary&format=csv");
		private static readonly Dictionary<string, string> TitleOverrides = new Dictionary<string, string>
		{
			// Title Overrides will likely only be necessary when creating new "(set)" pages that need to be disambiguated upon creation. While this could be done programatically, it's probably best not to, so that a human has verified that the page really should be created and that the existing page isn't malformed or something.
			["Grave Guardian"] = "Grave Guardian (set)"
		};
		#endregion

		#region Fields
		private readonly IDictionary<string, PageData> sets = new SortedDictionary<string, PageData>();
		private readonly IMediaWikiClient client;
		#endregion

		#region Constructors
		[JobInfo("Item Sets", "ESO")]
		public EsoItemSets(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			if (site.AbstractionLayer is IInternetEntryPoint entryPoint)
			{
				this.client = entryPoint.Client;
			}
			else
			{
				throw new InvalidCastException();
			}
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Update ESO Item Sets";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.SavePages(this.LogName, false);
			EsoGeneral.SetBotUpdateVersion(this, "itemset");
			this.Progress++;
		}

		protected override void JobCompleted()
		{
			EsoReplacer.ShowUnreplaced();
			base.JobCompleted();
		}

		protected override void BeforeLogging()
		{
			EsoReplacer.Initialize(this);
			this.StatusWriteLine("Fetching data");
			var csvData = this.client.Get(SetSummaryPage);
			var csvFile = new CsvFile();
			csvFile.ReadText(csvData, true);
			this.ProgressMaximum = csvFile.Count + 2;
			this.Progress++;

			this.StatusWriteLine("Updating");
			var sets = new List<PageData>();
			foreach (var row in csvFile)
			{
				if (!BadRows.Contains(int.Parse(row["id"])))
				{
					var setName = row["setName"].Replace(@"\'", "'", StringComparison.Ordinal);
					var bonusDescription = row["setBonusDesc"];
					if (bonusDescription[0] != '(')
					{
						this.Warn($"Set bonus for {setName} doesn't start with a bracket:{Environment.NewLine}{bonusDescription}");
					}

					sets.Add(new PageData(setName, bonusDescription));
				}
			}

			var titles = this.ResolveAndPopulateSets(sets);
			this.Pages.PageLoaded += this.SetLoaded;
			this.Pages.GetTitles(titles);
			this.Pages.PageLoaded -= this.SetLoaded;
			this.GenerateReport();
			this.Progress++;
		}
		#endregion

		#region Private Methods
		private static string BuildNewPage(string setName)
		{
			var sb = new StringBuilder();
			sb
				.Append("{{Trail|Sets}}{{Online Update}}{{Minimal}}\n")
				.Append("'''").Append(setName).Append("''' is a {{huh}}-rank [[Online:Sets|item set]] found in {{huh}}.\n\n")
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

			return sb.ToString();
		}

		private void GenerateReport()
		{
			this.WriteLine("== Item Sets With Non-Trivial Updates ==");
			foreach (var item in this.sets)
			{
				if (item.Value.IsNonTrivial)
				{
					this.WriteLine($"* {{{{Pl|Online:{item.Key}|{item.Value.SetName}|diff=cur}}}}");
				}
			}

			this.WriteLine();
		}

		private string PatchNumberReplacer(Match match)
		{
			var template = Template.Parse(match.Value);
			template.RemoveDuplicates();
			template.Remove("update");
			template.Remove("1");
			template.AddOrChange("type", "itemset");

			return template.ToString();
		}

		private TitleCollection ResolveAndPopulateSets(List<PageData> dbSets)
		{
			var catMembers = new TitleCollection(this.Site);
			catMembers.GetCategoryMembers("Online-Sets");

			var titles = new TitleCollection(this.Site); // These titles are known to exist.
			var uncheckedSets = new Dictionary<Title, PageData>(); // These titles are not in the category and need to be checked for redirects, etc.
			foreach (var set in dbSets)
			{
				if (!TitleOverrides.TryGetValue(set.SetName, out var setName))
				{
					setName = set.SetName;
				}

				if ((catMembers.FindTitle(UespNamespaces.Online, setName, true) ?? catMembers.FindTitle(UespNamespaces.Online, setName + " (set)", true)) is Title foundPage)
				{
					titles.Add(foundPage);
					try
					{
						this.sets.Add(foundPage.PageName, set);
					}
					catch (ArgumentException)
					{
						Debug.WriteLine($"Duplicate entry for {set.SetName} in raw data.");
					}
				}
				else
				{
					var newTitle = new Title(this.Site, UespNamespaces.Online, setName, true);
					uncheckedSets.Add(newTitle, set);
				}
			}

			var loadOptions = new PageLoadOptions(PageModules.Links | PageModules.Properties, true);
			var checkNewPages = new PageCollection(this.Site, loadOptions);
			checkNewPages.GetTitles(uncheckedSets.Keys);
			foreach (var title in uncheckedSets)
			{
				if (checkNewPages[title.Key.FullPageName] is Page page && page.Exists)
				{
					var resolved = false;
					if (page.IsDisambiguation)
					{
						foreach (var link in page.Links)
						{
							if (link.PageName.Contains(" (set)", StringComparison.OrdinalIgnoreCase))
							{
								titles.Add(link);
								this.sets.Add(link.PageName, title.Value);
								resolved = true;
								break;
							}
						}
					}

					if (!resolved)
					{
						this.Warn($"{page.FullPageName} exists but is neither a set nor a disambiguation to one. Please check!");
					}
				}
				else
				{
					titles.Add(title.Key);
					this.sets.Add(title.Key.PageName, title.Value);
					this.Warn($"{title.Key.FullPageName} does not exist and will be created.");
				}
			}

			return titles;
		}

		private void SetLoaded(object sender, Page page)
		{
			var pageData = this.sets[page.PageName];
			if (!page.Exists || string.IsNullOrEmpty(page.Text))
			{
				page.Text = BuildNewPage(pageData.SetName);
			}

			this.UpdatePageText(page, pageData);
		}

		private void UpdatePageText(Page page, PageData pageData)
		{
			const string marker = "<onlyinclude>";
			const string terminator = "</onlyinclude>";

			var start = page.Text.IndexOf(marker, StringComparison.Ordinal);
			var end = start >= 0 ? page.Text.IndexOf(terminator, start, StringComparison.Ordinal) : -1;
			if (start < 0 || end < 0)
			{
				this.Warn($"Delimiters not found on page {page.FullPageName}");
				return;
			}

			start += marker.Length;
			var sb = new StringBuilder();
			sb.Append('\n');
			var items = SetBonusRegex.Split(pageData.BonusDescription);
			if (items[0].Length > 0)
			{
				Debug.WriteLine("WTF?");
			}

			for (var itemNum = 1; itemNum < items.Length; itemNum += 2)
			{
				var itemName = items[itemNum].Trim(TextArrays.Parentheses);
				var desc = Regex.Replace(items[itemNum + 1].Trim(), "[\n ]+", " ");
				if (desc.StartsWith(page.PageName, StringComparison.Ordinal))
				{
					desc = desc.Substring(page.PageName.Length).TrimStart();
				}

				desc = EsoReplacer.ReplaceGlobal(desc, null);
				desc = EsoReplacer.ReplaceLink(desc);
				sb
					.Append("'''")
					.Append(itemName)
					.Append("''': ")
					.Append(desc)
					.Append("<br>\n");
			}

			sb.Remove(sb.Length - 5, 4);

			var text = sb.ToString();
			pageData.IsNonTrivial = EsoReplacer.CompareReplacementText(this, page.Text[start..end], text, page.FullPageName);
			text = EsoReplacer.FirstLinksOnly(this.Site, text);

			page.Text = OnlineUpdateRegex.Replace(page.Text.Substring(0, start), this.PatchNumberReplacer) + text + page.Text.Substring(end);
		}
		#endregion

		#region Private Classes
		private class PageData
		{
			public PageData(string setName, string bonusDescription)
			{
				this.SetName = setName;
				this.BonusDescription = bonusDescription;
			}

			public string BonusDescription { get; }

			public bool IsNonTrivial { get; set; }

			public string SetName { get; }
		}
		#endregion
	}
}
