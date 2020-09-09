namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	internal class EsoItemSets : EditJob
	{
		#region Static Fields
		private static readonly HashSet<int> BadRows = new HashSet<int> { 2666 };
		private static readonly Regex LineReplacer = new Regex(@"[\n ]+", RegexOptions.None, DefaultRegexTimeout);
		private static readonly Regex SetBonusRegex = new Regex(@"\(\s*(?<items>[1-6] items?)\s*\)\s*(?<text>.*?)\s*(?=(\([1-6] items?\)|\z))", RegexOptions.ExplicitCapture | RegexOptions.Singleline, DefaultRegexTimeout);
		private static readonly Uri SetSummaryPage = new Uri("http://esolog.uesp.net/viewlog.php?record=setSummary&format=csv");
		private static readonly Dictionary<string, string> TitleOverrides = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			// Title Overrides should only be necessary when creating new disambiguated "(set)" pages or when pages don't conform to the base/base (set) style. While this could be done programatically, it's probably best not to, so that a human has verified that the page really should be created and that the existing page isn't malformed or something.
			["Lady Thorn"] = "Lady Thorn (set)",
			["Senche-Raht's Grit"] = "Senche-raht's Grit",
		};
		#endregion

		#region Fields
		private readonly IDictionary<string, PageData> sets = new SortedDictionary<string, PageData>();
		private readonly IMediaWikiClient client;
		#endregion

		#region Constructors
		[JobInfo("Item Sets", "ESO")]
		public EsoItemSets(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.client = site.AbstractionLayer is IInternetEntryPoint entryPoint
				? entryPoint.Client
				: throw new InvalidCastException();
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
			var setList = new List<PageData>();
			foreach (var row in csvFile)
			{
				if (!BadRows.Contains(int.Parse(row["id"], CultureInfo.InvariantCulture)))
				{
					var setName = row["setName"].Replace(@"\'", "'", StringComparison.Ordinal);
					var bonusDescription = row["setBonusDesc"];
					if (bonusDescription[0] != '(')
					{
						this.Warn($"Set bonus for {setName} doesn't start with a bracket:{Environment.NewLine}{bonusDescription}");
					}

					var set = new PageData(setName, bonusDescription);
					if (TitleOverrides.TryGetValue(setName, out var pageName))
					{
						set.PageName = pageName;
					}

					setList.Add(set);
				}
			}

			this.ResolveAndPopulateSets(setList);
			var titles = new TitleCollection(this.Site);
			foreach (var set in this.sets)
			{
				titles.Add(UespNamespaces.Online, set.Key);
			}

			this.Pages.PageLoaded += this.SetLoaded;
			this.Pages.GetTitles(titles);
			this.Pages.PageLoaded -= this.SetLoaded;
			this.GenerateReport();
			this.Progress++;
		}
		#endregion

		#region Private Static Methods
		private static string? ConstructWarning(Page page, ICollection<ISimpleTitle> titles, string warningType)
		{
			if (titles.Count > 0)
			{
				var warning = new StringBuilder();
				warning
					.Append("Watch for ")
					.Append(warningType)
					.Append(" on ")
					.Append(page.FullPageName)
					.Append(": ");
				foreach (var link in titles)
				{
					warning
						.Append(link)
						.Append(", ");
				}

				warning.Remove(warning.Length - 2, 2);
				return warning.ToString();
			}

			return null;
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

		private void ResolveAndPopulateSets(List<PageData> dbSets)
		{
			var catTitles = new TitleCollection(this.Site);
			catTitles.GetCategoryMembers("Online-Sets");
			foreach (var set in dbSets)
			{
				catTitles.Add(new Title(this.Site[UespNamespaces.Online], set.PageName));
			}

			var loadOptions = new PageLoadOptions(PageModules.Info | PageModules.Properties, true);
			var catMembers = new PageCollection(this.Site, loadOptions);
			catMembers.GetTitles(catTitles);
			catMembers.Sort();

			var disambigs = new Dictionary<Title, PageData>();
			foreach (var set in dbSets)
			{
				var title = new Title(this.Site[UespNamespaces.Online], set.PageName); // Only used to normalize names. Some have underscores and other oddities.
				if (catMembers.TryGetValue(title.FullPageName + " (set)", out var foundPage) || catMembers.TryGetValue(title, out foundPage))
				{
					set.PageName = foundPage.PageName;
					if (foundPage.IsDisambiguation)
					{
						disambigs.Add(foundPage, set);
					}
					else
					{
						if (!foundPage.Exists)
						{
							this.Warn($"{foundPage.FullPageName} does not exist and will be created.");
						}

						this.AddToSets(set);
					}
				}
				else
				{
					throw new InvalidOperationException();
				}
			}

			loadOptions = new PageLoadOptions(PageModules.Links | PageModules.Properties, true);
			var checkNewPages = new PageCollection(this.Site, loadOptions);
			checkNewPages.GetTitles(disambigs.Keys);
			foreach (var title in disambigs)
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
								title.Value.PageName = link.PageName;
								this.AddToSets(title.Value);
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
			}
		}

		private void AddToSets(PageData set)
		{
			try
			{
				this.sets.Add(set.PageName, set);
			}
			catch (ArgumentException)
			{
				this.Warn($"Duplicate entry for {set.SetName} in raw data.");
			}
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
			var parser = new ContextualParser(page, InclusionType.Transcluded, false);
			var firstNode = parser.Nodes.First;
			if (firstNode == null || !(firstNode.Value is IgnoreNode ignoreNode && ignoreNode.Value.EndsWith("<onlyinclude>", StringComparison.OrdinalIgnoreCase)))
			{
				this.Warn($"Delimiters not found on page {page.FullPageName}");
				return;
			}

			var currentNode = firstNode.Next;
			var oldNodes = new NodeCollection(null);
			while (currentNode != null && !(currentNode.Value is IgnoreNode))
			{
				oldNodes.AddLast(currentNode.Value);
				var nextNode = currentNode.Next;
				parser.Nodes.Remove(currentNode);
				currentNode = nextNode;
			}

			var items = (IEnumerable<Match>)SetBonusRegex.Matches(pageData.BonusDescription);
			var usedList = new TitleCollection(this.Site);
			var sb = new StringBuilder();
			sb.Append('\n');
			foreach (var item in items)
			{
				var itemName = item.Groups["items"];
				var desc = item.Groups["text"].Value;
				desc = LineReplacer.Replace(desc, " ");
				if (desc.StartsWith(page.PageName, StringComparison.Ordinal))
				{
					desc = desc.Substring(page.PageName.Length).TrimStart();
				}

				sb
					.Append("'''")
					.Append(itemName)
					.Append("''': ")
					.Append(desc)
					.Append("<br>\n");
			}

			sb.Remove(sb.Length - 5, 4);
			var newNodes = NodeCollection.Parse(sb.ToString());
			firstNode.AddAfter(newNodes);

			EsoReplacer.ReplaceGlobal(parser.Nodes);
			EsoReplacer.ReplaceEsoLinks(parser.Nodes);
			EsoReplacer.ReplaceFirstLink(parser.Nodes, usedList);
			page.Text = parser.GetText() ?? string.Empty;

			var replacer = new EsoReplacer(this.Site);
			if (ConstructWarning(page, replacer.CheckNewLinks(oldNodes, parser.Nodes), "links") is string linkWarning)
			{
				this.Warn(linkWarning);
			}

			if (ConstructWarning(page, replacer.CheckNewTemplates(oldNodes, parser.Nodes), "templates") is string templateWarning)
			{
				this.Warn(templateWarning);
			}

			pageData.IsNonTrivial = replacer.IsNonTrivialChange(oldNodes, parser.Nodes);
		}
		#endregion

		#region Private Classes
		private class PageData
		{
			public PageData(string setName, string bonusDescription)
			{
				this.SetName = setName;
				this.PageName = setName;
				this.BonusDescription = bonusDescription;
			}

			public string BonusDescription { get; }

			public bool IsNonTrivial { get; set; }

			public string PageName { get; set; }

			public string SetName { get; }

			public override string ToString() => this.SetName;
		}
		#endregion
	}
}
