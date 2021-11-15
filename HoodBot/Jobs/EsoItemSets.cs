namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Data;
	using System.Text;
	using RobinHood70.CommonCode;
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
			"SELECT id, setName, setBonusDesc1, setBonusDesc2, setBonusDesc3, setBonusDesc4, setBonusDesc5, setBonusDesc6, setBonusDesc7\n" +
			"FROM setSummary\n";
		#endregion

		#region Static Fields
		private static readonly Dictionary<string, string> TitleOverrides = new(StringComparer.Ordinal)
		{
			// Title Overrides should only be necessary when creating new disambiguated "(set)" pages or when pages don't conform to the base/base (set) style. While this could be done programatically, it's probably best not to, so that a human has verified that the page really should be created and that the existing page isn't malformed or something.
			["Immolator Charr"] = "Immolator Charr (set)",
			["Magma Incarnate"] = "Magma Incarnate (set)",
			["Zoal the Ever-Wakeful"] = "Zoal the Ever-Wakeful (set)",
		};
		#endregion

		#region Fields
		private readonly KeyedCollection<Page, SetData> sets = new SetCollection();
		#endregion

		#region Constructors
		[JobInfo("Update Item Sets", "ESO Update")]
		public EsoItemSets(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Update ESO Item Sets";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			EsoReplacer.Initialize(this);

			this.StatusWriteLine("Fetching data");
			var allSets = this.GetSetPages();
			TitleCollection titles = new(this.Site);
			foreach (var set in allSets)
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

			// Needs to be after update, since update modifies item's IsNonTrivial property.
			allSets.Sort((item, item2) => string.CompareOrdinal(item.Name, item2.Name));
			this.GenerateReport(allSets);
		}

		protected override void JobCompleted()
		{
			EsoReplacer.ShowUnreplaced();
			base.JobCompleted();
		}

		protected override void Main()
		{
			this.SavePages(this.LogName, false);
			EsoSpace.SetBotUpdateVersion(this, "itemset");
		}
		#endregion

		#region Private Static Methods
		private static List<SetData> GetSetData()
		{
			List<SetData> allSets = new();
			foreach (var row in EsoLog.RunQuery(Query))
			{
				allSets.Add(new SetData(row));
			}

			return allSets;
		}
		#endregion

		#region Private Methods
		private void BuildNewPages(List<SetData> allSets)
		{
			foreach (var set in allSets)
			{
				if (set.Page is null || !set.Page.Exists)
				{
					this.Warn($"New Page: {set.Name}");
					set.Page = set.BuildNewPage(TitleFactory.Direct(this.Site, UespNamespaces.Online, set.Name));
				}
			}
		}

		private void GenerateReport(List<SetData> allSets)
		{
			StringBuilder sb = new();
			foreach (var item in allSets)
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

		private List<SetData> GetSetPages()
		{
			var allSets = GetSetData();
			this.MatchCategoryPages(allSets);
			this.MatchUnresolvedPages(allSets);
			this.BuildNewPages(allSets);

			return allSets;
		}

		private void MatchCategoryPages(List<SetData> allSets)
		{
			PageCollection catPages = new(this.Site, new PageLoadOptions(PageModules.Info, true));
			catPages.GetCategoryMembers("Online-Sets", CategoryMemberTypes.Page, false);
			this.UpdateSetPages(allSets, catPages);
		}

		private void MatchUnresolvedPages(List<SetData> allSets)
		{
			TitleCollection titles = new(this.Site);
			foreach (var set in allSets)
			{
				if (set.Page is null)
				{
					foreach (var setName in set.AllNames)
					{
						titles.Add(UespNamespaces.Online, setName);
					}
				}
			}

			this.ResolveDisambiguations(allSets, titles);
		}

		private void ResolveDisambiguations(List<SetData> allSets, TitleCollection newTitles)
		{
			if (newTitles.Count == 0)
			{
				return;
			}

			List<Page> removePages = new();
			List<Title> addTitles = new();
			PageCollection pages = new(this.Site, new PageLoadOptions(PageModules.Info | PageModules.Links | PageModules.Properties, true));
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

				PageCollection addPages = new(this.Site, PageModules.Info);
				addPages.GetTitles(addTitles);
				foreach (var page in addPages)
				{
					pages.Add(page);
				}
			}

			this.UpdateSetPages(allSets, pages);
		}

		private void SetLoaded(object sender, Page page)
		{
			var setData = this.sets[page];
			if (!page.Exists && setData.Page is not null)
			{
				// TODO: Check this, it's definitely not the optimal way of doing it. Why do we have loaded pages and a page object as part of .sets?
				page.Text = setData.Page.Text;
			}

			ContextualParser oldPage = new(page, InclusionType.Transcluded, false);
			if (oldPage.Nodes.Count < 2 || !(
					oldPage.Nodes[0] is IIgnoreNode firstNode &&
					oldPage.Nodes[^1] is IIgnoreNode lastNode))
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
			ContextualParser newPage = new(page, sb.ToString());
			EsoReplacer.ReplaceGlobal(newPage.Nodes);
			EsoReplacer.ReplaceEsoLinks(this.Site, newPage.Nodes);
			EsoReplacer.ReplaceFirstLink(newPage.Nodes, usedList);

			// Now that we're done parsing, re-add the IgnoreNodes.
			newPage.Nodes.Insert(0, firstNode);
			newPage.Nodes.Add(lastNode);

			EsoReplacer replacer = new(this.Site);
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

			setData.IsNonTrivial = replacer.IsNonTrivialChange(oldPage, newPage);
			page.Text = newPage.ToRaw();
		}

		private void UpdateSetPageAnyCase(SetData set, PageCollection setMembers)
		{
			foreach (var setName in set.AllNames)
			{
				foreach (var setMember in setMembers)
				{
					if (string.Compare(setMember.PageName, setName, true, this.Site.Culture) == 0 && string.Compare(setMember.PageName, setName, false, this.Site.Culture) != 0)
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

		private void UpdateSetPages(List<SetData> allSets, PageCollection setMembers)
		{
			foreach (var set in allSets)
			{
				if (set.Page is null)
				{
					if (TitleOverrides.TryGetValue(set.Name, out var overrideName))
					{
						TitleFactory? checkTitle = TitleFactory.FromName(this.Site, UespNamespaces.Online, overrideName);
						set.Page = setMembers.TryGetValue(checkTitle, out var foundPage)
							? foundPage
							: set.BuildNewPage(TitleFactory.Direct(this.Site, UespNamespaces.Online, set.Name));
					}
					else
					{
						foreach (var setName in set.AllNames)
						{
							TitleFactory? checkTitle = TitleFactory.FromName(this.Site, UespNamespaces.Online, setName);
							if (setMembers.TryGetValue(checkTitle, out var foundPage) &&
								foundPage.Exists)
							{
								set.Page = foundPage;
								break;
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

			protected override Page GetKeyForItem(SetData item) => item.Page ?? throw new InvalidOperationException("Item Page is null!");
		}

		private sealed class SetData
		{
			#region Constructors
			public SetData(IDataRecord row)
			{
				this.Name = (string)row.NotNull(nameof(row))["setName"];
				for (var c = '1'; c <= '7'; c++)
				{
					var bonusDesc = (string)row["setBonusDesc" + c];
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

			public IEnumerable<string> AllNames
			{
				get
				{
					yield return this.Name + " (set)";
					yield return this.Name;
				}
			}

			public List<(string ItemCount, string Text)> BonusDescriptions { get; } = new();

			public bool IsNonTrivial { get; set; }

			public string Name { get; }

			public Page? Page { get; set; }
			#endregion

			#region Public Methods
			public Page BuildNewPage(ISimpleTitle title)
			{
				StringBuilder sb = new();
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

				return TitleFactory.DirectNormalized(title).ToNewPage(sb.ToString());
			}
			#endregion

			#region Public Override Methods
			public override string ToString() => $"{this.Name} ({this.Page?.FullPageName ?? "NO PAGE"})";
			#endregion
		}
		#endregion
	}
}
