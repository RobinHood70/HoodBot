namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.Design;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

// TODO: This job is a serious mess and needs an overhaul. Some has already been done, but it needs a lot more. There's also a lot of page data being loaded from the wiki. Can this be done better by checking a category (either before loading the page data or instead of it)?
internal sealed class EsoNpcs : EditJob
{
	#region Fields
	private readonly NpcCollection npcCollection = [];
	private readonly Dictionary<Title, NpcData> pageNpcs = [];
	private readonly bool allowUpdates;
	#endregion

	#region Constructors
	[JobInfo("Create Missing NPCs", "ESO Update")]
	public EsoNpcs(JobManager jobManager, [JobParameter(DefaultValue = false)] bool allowUpdates)
		: base(jobManager)
	{
		this.allowUpdates = allowUpdates;
		//// jobManager.ShowDiffs = false;
		if (this.Results is PageResultHandler pageResults)
		{
			var title = pageResults.Title;
			pageResults.Title = TitleFactory.FromValidated(title.Namespace, title.PageName + "/ESO NPCs");
			pageResults.SaveAsBot = false;
		}

		// TODO: Rewrite Mod Header handling to be more intelligent.
		this.StatusWriteLine("DON'T FORGET TO UPDATE MOD HEADER!");
	}
	#endregion

	#region Public Override Properties
	public override string LogName => (this.allowUpdates ? "Update" : "Create missing") + " ESO NPC pages";
	#endregion

	#region Protected Override Methods
	protected override void BeforeLoadPages()
	{
		// TODO: Update mode could load pages with no loc or {{huh}} via MetaTemplate variables.
		this.StatusWriteLine("Getting NPC data");
		this.GetNpcPages();
		this.npcCollection.GetLocations(this.Site);

		this.StatusWriteLine("Getting place data");
		var places = EsoSpace.GetPlaces(this.Site);
		this.npcCollection.ParseLocations(places);
		foreach (var npc in this.npcCollection)
		{
			npc.TrimPlaces();
		}
	}

	protected override string GetEditSummary(Page page) => this.LogName;

	protected override bool GetIsMinorEdit(Page page) => false;

	protected override void LoadPages()
	{
		var npcPages = new TitleCollection(this.Site);
		foreach (var npc in this.npcCollection)
		{
			if (npc.Title is Title title)
			{
				npcPages.Add(title);
			}
		}

		this.Pages.GetTitles(npcPages);
	}

	protected override void PageLoaded(Page page)
	{
		if (page.IsMissing || string.IsNullOrWhiteSpace(page.Text))
		{
			page.Text = this.NewPageText(this.pageNpcs[page.Title]);
			page.SetMinimalStartTimestamp();
		}

		if (this.allowUpdates)
		{
			var npc = this.pageNpcs[page.Title];
			var placeInfo = EsoSpace.PlaceInfo;

			var parser = new SiteParser(page);
			if (parser.FindSiteTemplate("Online NPC Summary") is ITemplateNode template)
			{
				UpdateLocations(npc, template, parser.Factory, placeInfo);
				parser.UpdatePage();
				this.Pages.Add(page);
			}
			else
			{
				throw new InvalidOperationException();
			}
		}
	}
	#endregion

	#region Private Static Methods

	private static int NpcComparer((NpcData Npc, string Issue) x, (NpcData Npc, string Issue) y) => x.Npc.Title is Title xTitle
			? y.Npc.Title is Title yTitle
				? TitleComparer.Instance.Compare(xTitle, yTitle)
				: 1
			: y.Npc.Title is null
				? 0
				: -1;

	private static string NpcToWikiText(NpcData npc)
	{
		var sb = new StringBuilder();
		sb
			.Append("\n* Name: ")
			.Append(npc.Name)
			.Append("\n* Gender: ")
			.Append(npc.GenderText);
		if (!string.IsNullOrWhiteSpace(npc.LootType))
		{
			sb
				.Append("\n* Loot Type: ")
				.Append(npc.LootType);
		}

		var npcPlaces = string.Join(", ", npc.Places);
		if (!string.IsNullOrWhiteSpace(npcPlaces))
		{
			sb.Append("\n* Known Locations: ").Append(npcPlaces);
		}

		var sbLoc = sb.Length;
		foreach (var place in npc.UnknownLocations)
		{
			sb
				.Append(", ")
				.Append(place.Key.TitleName);
		}

		if (sb.Length > sbLoc)
		{
			sb.Insert(sbLoc, "\n* Unknown Locations: ");
		}

		return sb.ToString();
	}

	private static void UpdateLocations(NpcData npc, ITemplateNode template, IWikiNodeFactory factory, IEnumerable<PlaceInfo> placeInfos)
	{
		foreach (var (placeType, paramName, _, variesCount) in placeInfos)
		{
			var placeText = npc.GetParameterValue(placeType, variesCount);
			InsertLocation(template, factory, paramName, placeText);
		}

		if (npc.UnknownLocations.Count > 0)
		{
			string locText;
			if (npc.UnknownLocations.Count < 10)
			{
				SortedSet<string> list = new(StringComparer.Ordinal);
				foreach (var place in npc.UnknownLocations)
				{
					list.Add(place.Key.TitleName);
				}

				locText = string.Join(", ", list);
			}
			else
			{
				locText = "Varies";
			}

			InsertLocation(template, factory, "loc", locText);
		}

		static void InsertLocation(ITemplateNode template, IWikiNodeFactory factory, string name, string locText)
		{
			if (locText.Length > 0)
			{
				locText += '\n';
				if (template.Find(name) is IParameterNode loc)
				{
					var value = loc.GetValue();
					if (!value.OrdinalEquals(locText))
					{
						loc.SetValue(loc.IsNullOrWhitespace() ? locText : (value + ", " + locText), ParameterFormat.Copy);
					}
				}
				else
				{
					var index = template.FindIndex("race");
					template.Parameters.Insert(index, factory.ParameterNodeFromParts(name, locText));
				}
			}
		}
	}
	#endregion

	#region Private Methods

	private PageCollection GetCheckPages(NpcCollection npcs)
	{
		TitleCollection checkTitles = new(this.Site);
		foreach (var npc in npcs)
		{
			checkTitles.AddRange(UespNamespaces.Online, npc.DataName);
		}

		PageCollection checkPages = new(this.Site, PageModules.Info | PageModules.Properties, true);
		checkPages.GetTitles(checkTitles);
		return checkPages;
	}

	private TitleCollection GetExistingTitles()
	{
		TitleCollection existingTitles = new(this.Site);
		existingTitles.GetCategoryMembers("Online-NPCs", CategoryMemberTypes.Page, false);
		existingTitles.GetCategoryMembers("Online-Creatures-All", CategoryMemberTypes.Page, false);
		return existingTitles;
	}

	private void GetNpcPages()
	{
		var npcs = EsoLog.GetNpcs();
		foreach (var dupe in npcs.Duplicates)
		{
			this.Warn($"Warning: an NPC with the name \"{dupe.DataName}\" exists more than once in the database!");
		}

		var loadNpcs = this.GetNpcsToLoad(npcs);
		var loadPages = this.GetPages(loadNpcs);

		List<(NpcData, string)> issues = [];
		foreach (var npc in loadNpcs)
		{
			if (npc.Title is null || !loadPages.TryGetValue(npc.Title, out var page))
			{
				continue;
			}

			npc.Title = page.Title;
			var issue = page switch
			{
				Page when page.IsDisambiguation == true => "is a disambiguation with no clear NPC link",
				Page when page.IsRedirect => "is a redirect to a content page without an Online NPC Summary",
				Page when !this.allowUpdates && page.IsMissing && page.PreviouslyDeleted => "was previously deleted",
				_ => null
			};

			if (issue == null)
			{
				SiteParser parsed = new(page);
				var template = parsed.FindSiteTemplate("Online NPC Summary");
				if (this.allowUpdates)
				{
					if (template?.Find("city").IsNullOrWhitespace() == true &&
						template.Find("settlement").IsNullOrWhitespace() &&
						template.Find("house").IsNullOrWhitespace() &&
						template.Find("ship").IsNullOrWhitespace() &&
						template.Find("store").IsNullOrWhitespace() &&
						template.Find("loc") is IParameterNode loc &&
						(loc.IsNullOrWhitespace() || loc.GetValue().OrdinalICEquals("{{huh}}")))
					{
						this.npcCollection.Add(npc);
					}
				}
				else if (template is null)
				{
					if (page.IsMissing)
					{
						this.npcCollection.Add(npc);
					}
					else
					{
						issue = "is already a content page without an Online NPC Summary";
					}
				}
			}

			if (issue != null)
			{
				issues.Add((npc, issue));
			}
			else
			{
				this.pageNpcs.Add(page.Title, npc);
			}
		}

		issues.Sort(NpcComparer);
		this.WriteLine("{| class=\"wikitable sortable\"");
		this.WriteLine("! Page !! Issue !! NPC Data");
		foreach (var (npc, issue) in issues)
		{
			this.WriteLine("|-");
			this.WriteLine(npc.Title is Title title
				? $"| {SiteLink.ToText(title, LinkFormat.LabelName)}"
				: "|");
			this.WriteLine($"| {issue}");
			this.WriteLine("| " + NpcToWikiText(npc));
		}

		this.WriteLine("|}");
	}

	private NpcCollection GetNpcsToLoad(NpcCollection npcs)
	{
		var existingTitles = this.GetExistingTitles();
		var checkPages = this.GetCheckPages(npcs);
		NpcCollection loadNpcs = [];
		foreach (var npc in npcs)
		{
			var title = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Online], npc.DataName);
			if (checkPages.TitleMap.TryGetValue(title.FullPageName(), out var redirect))
			{
				npc.Name = redirect.Title.PageName;
			}
			else if (checkPages.TryGetValue(title, out var page) && page.IsDisambiguation == true)
			{
				SiteParser parser = new(page);
				foreach (var linkNode in parser.LinkNodes)
				{
					var disambig = SiteLink.FromLinkNode(this.Site, linkNode);
					if (existingTitles.TryGetValue(disambig, out var disambigPage))
					{
						npc.Name = disambigPage.PageName;
						break;
					}
				}
			}

			var exists = checkPages.TryGetValue(title, out var existCheck) && existCheck.Exists;
			if (!exists || (exists && this.allowUpdates))
			{
				npc.Title = title;
				loadNpcs.Add(npc);
			}
		}

		return loadNpcs;
	}

	private PageCollection GetPages(NpcCollection loadNpcs)
	{
		TitleCollection loadTitles = new(this.Site);
		foreach (var npc in loadNpcs)
		{
			if (npc.Title is Title title)
			{
				loadTitles.Add(title);
			}
		}

		PageCollection loadPages = new(this.Site, PageModules.Default | PageModules.DeletedRevisions | PageModules.Properties, true);
		loadPages.GetTitles(loadTitles);
		loadPages.Sort();
		return loadPages;
	}

	private string NewPageText(NpcData npc)
	{
		List<(string?, string)> parameters =
		[
			("id", npc.Id.ToStringInvariant()),
			("image", string.Empty),
			("imgdesc", string.Empty),
			("race", string.Empty),
			("gender", npc.GenderText),
			("difficulty", npc.Difficulty > 0 ? npc.Difficulty.ToString(this.Site.Culture) : string.Empty),
			("reaction", npc.Reaction),
			("pickpocket", npc.PickpocketDifficulty > PickpocketDifficulty.Unknown ? npc.PickpocketDifficultyText : string.Empty),
			("loottype", npc.LootType),
			("faction", string.Empty)
		];

		var factory = new SiteNodeFactory(this.Site);
		var template = factory.TemplateNodeFromParts("Online NPC Summary", true, parameters);
		UpdateLocations(npc, template, factory, EsoSpace.PlaceInfo);

		return new StringBuilder()
			.Append("{{Minimal|NPC}}{{Mod Header|Gold Road}}")
			.AppendLine(WikiTextVisitor.Raw(template))
			.AppendLine()
			.AppendLine("<!-- Instructions: Provide an initial sentence summarizing the NPC (race, job, where they live). Subsequent paragraphs provide additional information about the NPC, such as related NPCs, schedule, equipment, etc. Note that quest-specific information DOES NOT belong on this page, but instead goes on the appropriate quest page. Spoilers should be avoided.-->")
			.AppendLine("{{NewLeft}}")
			.AppendLine()
			.AppendLine("<!--Instructions: If this NPC is related to any quests, replace \"Quest Name\" with the quest's name.--><!--")
			.AppendLine("==Related Quests==")
			.AppendLine("* {{Quest Link|Quest Name}}")
			.AppendLine("--><!--Instructions: Add any miscellaneous notes about the NPC here, with a bullet for each note.--><!--")
			.AppendLine("==Notes==")
			.AppendLine("* Add note here")
			.AppendLine("--><!--Instructions: Add any bugs related to the NPC here using the format below.--><!--")
			.AppendLine("==Bugs==")
			.AppendLine("{{Bug|Bug description}}")
			.AppendLine("** Workaround")
			.AppendLine("-->")
			.AppendLine()
			.AppendLine("{{Stub|NPC}}")
			.ToString();
	}
	#endregion
}