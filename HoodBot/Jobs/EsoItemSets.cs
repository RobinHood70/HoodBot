namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;

	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	internal sealed class EsoItemSets : EditJob
	{
		#region Static Fields
		private static readonly HashSet<int> BadRows = new(); // None atm.
		private static readonly Regex SetBonusRegex = new(@"\(\s*(?<items>[1-6] items?)\s*\)\s*(?<text>.*?)\s*(?=(\([1-6] items?\)|\z))", RegexOptions.ExplicitCapture | RegexOptions.Singleline, DefaultRegexTimeout);
		private static readonly Uri SetSummaryPage = new("http://esolog.uesp.net/viewlog.php?record=setSummary&format=csv");
		private static readonly Dictionary<string, string> TitleOverrides = new(StringComparer.Ordinal)
		{
			// Title Overrides should only be necessary when creating new disambiguated "(set)" pages or when pages don't conform to the base/base (set) style. While this could be done programatically, it's probably best not to, so that a human has verified that the page really should be created and that the existing page isn't malformed or something.
			["Immolator Charr"] = "Immolator Charr (set)",
			["Zoal the Ever-Wakeful"] = "Zoal the Ever-Wakeful (set)",
		};
		#endregion

		#region Fields
		private readonly IMediaWikiClient client;
		private readonly List<SetData> allSets = new();
		private readonly KeyedCollection<Page, SetData> sets = new SetCollection();
		#endregion

		#region Constructors
		[JobInfo("Item Sets", "ESO")]
		public EsoItemSets(JobManager jobManager)
			: base(jobManager) => this.client = this.Site.AbstractionLayer is IInternetEntryPoint entryPoint
				? entryPoint.Client
				: throw new InvalidOperationException();
		#endregion

		#region Public Override Properties
		public override string LogName => "Update ESO Item Sets";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			EsoReplacer.Initialize(this);

			this.StatusWriteLine("Fetching data");
			this.GetSetPages();
			var titles = new TitleCollection(this.Site);
			foreach (var set in this.allSets)
			{
				if (set.Page is not null)
				{
					this.sets.Add(set);
					titles.Add(set.Page);
				}
				else
				{
					throw new InvalidOperationException($"The page for set {set.Name} is null!");
				}
			}

			this.StatusWriteLine("Updating");
			this.Pages.PageLoaded += this.SetLoaded;
			this.Pages.GetTitles(titles);
			this.Pages.PageLoaded -= this.SetLoaded;
			this.GenerateReport();
		}

		protected override void JobCompleted()
		{
			EsoReplacer.ShowUnreplaced();
			base.JobCompleted();
		}

		protected override void Main()
		{
			this.SavePages(this.LogName, false);
			EsoGeneral.SetBotUpdateVersion(this, "itemset");
		}
		#endregion

		#region Private Methods
		private void BuildNewPages()
		{
			foreach (var set in this.allSets)
			{
				if (set.Page is null)
				{
					this.Warn($"New Page: {set.Name}");
					set.BuildNewPage(new Title(this.Site.Namespaces[UespNamespaces.Online], set.Name));
				}
			}
		}

		private void GenerateReport()
		{
			var sb = new StringBuilder();
			foreach (var item in this.allSets)
			{
				if (item.Page is null)
				{
					throw new InvalidOperationException($"{item.Name}'s Page property is null. This should never happen.");
				}

				if (item.IsNonTrivial)
				{
					sb
						.Append("* {{Pl|")
						.Append(item.Page!.FullPageName)
						.Append('|')
						.Append(item.Name)
						.AppendLine("|diff=cur}}");
				}
			}

			if (sb.Length > 0)
			{
				this.WriteLine("== Item Sets With Non-Trivial Updates ==");
				this.WriteLine(sb.ToString().Replace("\r", string.Empty, StringComparison.Ordinal));
			}
		}

		private void GetSetPages()
		{
			this.GetSetsFromCsv();
			this.MatchCategoryPages();
			this.MatchUnresolvedPages();
			this.BuildNewPages();
		}

		private void GetSetsFromCsv()
		{
			var csvFile = new CsvFile();
			var csvData = this.client.Get(SetSummaryPage);
			csvFile.ReadText(csvData, true);
			foreach (var row in csvFile)
			{
				if (!BadRows.Contains(int.Parse(row["id"], CultureInfo.InvariantCulture)))
				{
					var setName = row["setName"].Replace(@"\'", "'", StringComparison.Ordinal);
					var bonusDescription = row["setBonusDesc"];
					if (bonusDescription[0] != '(')
					{
						throw new InvalidOperationException($"Set bonus for {setName} doesn't start with a bracket:{Environment.NewLine}{bonusDescription}");
					}

					this.allSets.Add(new SetData(setName, bonusDescription));
				}
			}
		}

		private void MatchCategoryPages()
		{
			var catPages = new PageCollection(this.Site, new PageLoadOptions(PageModules.Info, true));
			catPages.GetCategoryMembers("Online-Sets", CategoryMemberTypes.Page, false);
			this.UpdateSetPages(catPages);
		}

		private void MatchUnresolvedPages()
		{
			var titles = new TitleCollection(this.Site);
			foreach (var set in this.allSets)
			{
				if (set.Page == null)
				{
					foreach (var setName in set.AllNames)
					{
						titles.Add(UespNamespaces.Online, setName);
					}
				}
			}

			this.ResolveDisambiguations(titles);
		}

		private void ResolveDisambiguations(TitleCollection newTitles)
		{
			if (newTitles.Count == 0)
			{
				return;
			}

			var removePages = new List<Page>();
			var addTitles = new List<Title>();
			var pages = new PageCollection(this.Site, new PageLoadOptions(PageModules.Info | PageModules.Links | PageModules.Properties, true));
			pages.GetTitles(newTitles);
			foreach (var page in pages)
			{
				if (page.IsDisambiguation == true)
				{
					var resolved = false;
					foreach (var link in page.Links)
					{
						if (link.PageName.Contains(" (set)", StringComparison.OrdinalIgnoreCase))
						{
							resolved = true;
							removePages.Add(page);
							addTitles.Add(link);
							break;
						}
					}

					if (!resolved)
					{
						this.Warn($"{page.FullPageName} exists but is neither a set nor a disambiguation to one. Please check this and add the title to TitleOverrides to specify the desired page name.");
					}
				}
			}

			if (removePages.Count > 0)
			{
				foreach (var page in removePages)
				{
					pages.Remove(page);
				}

				var addPages = new PageCollection(this.Site, PageModules.Info);
				addPages.GetTitles(addTitles);
				pages.Add(addPages);
			}

			this.UpdateSetPages(pages);
		}

		private void SetLoaded(object sender, Page page)
		{
			var pageData = this.sets[page];
			if (!page.Exists || string.IsNullOrEmpty(page.Text))
			{
				throw new InvalidOperationException();
			}

			this.UpdatePageText(page, pageData);
		}

		private void UpdatePageText(Page page, SetData pageData)
		{
			var oldPage = new ContextualParser(page, InclusionType.Transcluded, false);
			if (oldPage.Nodes.Count < 2 || !(
					oldPage.Nodes[0] is IIgnoreNode firstNode &&
					oldPage.Nodes[^1] is IIgnoreNode lastNode))
			{
				this.Warn($"Delimiters not found on page {page.FullPageName}\n");
				return;
			}

			var items = (IEnumerable<Match>)SetBonusRegex.Matches(pageData.BonusDescription);
			var usedList = new TitleCollection(this.Site);
			var sb = new StringBuilder();
			sb.Append('\n');
			foreach (var item in items)
			{
				var itemName = item.Groups["items"];
				var desc = item.Groups["text"].Value;
				desc = RegexLibrary.WhitespaceToSpace(desc);
				if (desc.StartsWith(page.PageName, StringComparison.Ordinal))
				{
					desc = desc[page.PageName.Length..].TrimStart();
				}

				sb
					.Append("'''")
					.Append(itemName)
					.Append("''': ")
					.Append(desc)
					.Append("<br>\n");
			}

			sb.Remove(sb.Length - 5, 4);
			var newPage = new ContextualParser(page, sb.ToString());
			EsoReplacer.ReplaceGlobal(newPage.Nodes);
			EsoReplacer.ReplaceEsoLinks(this.Site, newPage.Nodes);
			EsoReplacer.ReplaceFirstLink(newPage.Nodes, usedList);

			// Now that we're done parsing, re-add the IgnoreNodes.
			newPage.Nodes.Insert(0, firstNode);
			newPage.Nodes.Add(lastNode);

			var replacer = new EsoReplacer(this.Site);
			var newLinks = replacer.CheckNewLinks(oldPage, newPage);
			if (newLinks.Count > 0)
			{
				this.Warn(EsoReplacer.ConstructWarning(oldPage, newPage, newLinks, "links"));
			}

			var newTemplates = replacer.CheckNewTemplates(oldPage, newPage);
			if (newTemplates.Count > 0)
			{
				this.Warn(EsoReplacer.ConstructWarning(oldPage, newPage, newTemplates, "templates"));
			}

			pageData.IsNonTrivial = replacer.IsNonTrivialChange(oldPage, newPage);
			page.Text = newPage.ToRaw();
		}

		private void UpdateSetPageAnyCase(SetData set, PageCollection setMembers)
		{
			foreach (var setName in set.AllNames)
			{
				foreach (var setMember in setMembers)
				{
					if (string.Compare(setMember.PageName, setName, true, this.Site.Culture) == 0)
					{
						this.Warn($"Substituted {setMember.PageName} for {setName}");
						set.Page = setMember;
						break;
					}
				}

				if (set.Page is not null)
				{
					break;
				}
			}
		}

		private void UpdateSetPages(PageCollection setMembers)
		{
			foreach (var set in this.allSets)
			{
				if (set.Page is null)
				{
					if (TitleOverrides.TryGetValue(set.Name, out var overrideName))
					{
						var checkTitle = Title.FromName(this.Site, UespNamespaces.Online, overrideName);
						set.Page = setMembers.TryGetValue(checkTitle, out var foundPage)
							? foundPage
							: throw new InvalidOperationException($"TitleOverride for {set.Name} => {overrideName} doesn't match any known sets.");
					}
					else
					{
						foreach (var setName in set.AllNames)
						{
							var checkTitle = Title.FromName(this.Site, UespNamespaces.Online, setName);
							if (setMembers.TryGetValue(checkTitle, out var foundPage))
							{
								if (foundPage.Exists)
								{
									set.Page = foundPage;
									break;
								}
							}
						}

						if (set.Page is null)
						{
							this.UpdateSetPageAnyCase(set, setMembers);
						}
					}
				}
			}
		}
		#endregion

		#region Private Classes
		private sealed class SetCollection : KeyedCollection<Page, SetData>
		{
			public SetCollection()
				: base(SimpleTitleEqualityComparer.Instance)
			{
			}

			protected override Page GetKeyForItem(SetData item)
			{
				return item.Page ?? throw new InvalidOperationException("Item Page is null!");
			}
		}

		private sealed class SetData
		{
			#region Constructors
			public SetData(string setName, string bonusDescription)
			{
				ThrowNull(setName, nameof(setName));
				ThrowNull(bonusDescription, nameof(bonusDescription));
				this.Name = setName;
				this.BonusDescription = bonusDescription;
			}
			#endregion

			#region Public Properties

			public IEnumerable<string> AllNames
			{
				get
				{
					yield return this.Name + " (set)";
					yield return this.Name;
				}
			}

			public string BonusDescription { get; }

			public bool IsNonTrivial { get; set; }

			public string Name { get; }

			public Page? Page { get; set; }
			#endregion

			#region Public Methods
			public void BuildNewPage(Title pageName)
			{
				var sb = new StringBuilder();
				sb
					.Append("{{Trail|Sets}}{{Online Update}}{{Minimal}}\n")
					.Append("'''").Append(this.Name).Append("''' is a {{huh}}-rank [[Online:Sets|item set]] found in {{huh}}.\n\n")
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

				this.Page = new Page(pageName)
				{
					Text = sb.ToString()
				};
			}
			#endregion

			#region Public Override Methods
			public override string ToString() => $"{this.Name} ({this.Page?.FullPageName ?? "NO PAGE"})";
			#endregion
		}
		#endregion
	}
}
