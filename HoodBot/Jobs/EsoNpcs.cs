namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Parser.Basic;
	using static RobinHood70.CommonCode.Globals;

	internal sealed class EsoNpcs : EditJob
	{
		#region Fields
		private readonly bool updateMode;
		#endregion

		#region Constructors
		[JobInfo("Create missing NPCs", "ESO")]
		public EsoNpcs(JobManager jobManager, [JobParameter(DefaultValue = false)] bool updateMode)
			: base(jobManager)
		{
			this.updateMode = updateMode;
			this.SetResultDescription("Existing ESO NPC pages");
		}
		#endregion

		#region Protected Override Properties
		public override string LogName => (this.updateMode ? "Update" : "Create missing") + "ESO NPC pages";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			// TODO: This could be optimized further. Regular mode could load the page collection directly rather than by title; update mode could load pages with no loc or {{huh}} via MetaTemplate variables.
			this.StatusWriteLine("Getting NPC data");
			var allPages = new PageCollection(this.Site, new PageLoadOptions(PageModules.Default | PageModules.DeletedRevisions | PageModules.Properties, true));
			allPages.GetCategoryMembers("Online-NPCs", CategoryMemberTypes.Page, false);
			allPages.GetCategoryMembers("Online-Creatures-All", CategoryMemberTypes.Page, false);
			var npcCollection = this.GetNpcPages(allPages);
			var filteredNpcs = this.updateMode ? FilterNpcsUpdate(npcCollection) : this.FilterNpcs(npcCollection);
			EsoGeneral.GetNpcLocations(filteredNpcs);

			this.StatusWriteLine("Getting place data");
			var places = EsoGeneral.GetPlaces(this.Site);
			var placeInfo = EsoGeneral.PlaceInfo;
			EsoGeneral.ParseNpcLocations(filteredNpcs, places);
			foreach (var npc in filteredNpcs)
			{
				npc.TrimPlaces();
			}

			if (this.updateMode)
			{
				foreach (var npc in filteredNpcs)
				{
					// TODO: We end up parsing the page twice with this design, one in FilterNpcsUpdate and once here. See if there's something that can be done to optimize this.
					if (npc.Page is Page page &&
						new ContextualParser(page) is var parser &&
						parser.FindTemplate("Online NPC Summary") is ITemplateNode template)
					{
						UpdateLocations(npc, template, parser.Nodes.Factory, placeInfo);
						page.Text = parser.ToRaw();
						this.Pages.Add(page);
					}
					else
					{
						throw new InvalidOperationException();
					}
				}
			}
			else
			{
				foreach (var npc in filteredNpcs)
				{
					if (npc.Page is Page page)
					{
						page.Text = NewPageText(npc, this.Site.Culture, placeInfo);
						page.SetMinimalStartTimestamp();
						this.Pages.Add(page);
					}
					else
					{
						throw new InvalidOperationException();
					}
				}
			}
		}

		protected override void Main() => this.SavePages(this.LogName, false);
		#endregion

		#region Private Static Methods

		private static NpcCollection FilterNpcsUpdate(NpcCollection npcs)
		{
			var retval = new NpcCollection();
			foreach (var npc in npcs)
			{
				if (npc.Page is Page page)
				{
					var parsed = new ContextualParser(page);
					if (parsed.FindTemplate("Online NPC Summary") is ITemplateNode template &&
						template.Find("city").IsNullOrWhitespace() &&
						template.Find("settlement").IsNullOrWhitespace() &&
						template.Find("house").IsNullOrWhitespace() &&
						template.Find("ship").IsNullOrWhitespace() &&
						template.Find("store").IsNullOrWhitespace() &&
						template.Find("loc") is IParameterNode loc &&
						(loc.IsNullOrWhitespace() || string.Equals(loc.Value.ToValue(), "{{huh}}", StringComparison.OrdinalIgnoreCase)))
					{
						retval.Add(npc);
					}
				}
			}

			return retval;
		}

		private static string NewPageText(NpcData npc, CultureInfo culture, IEnumerable<PlaceInfo> placeInfo)
		{
			var parameters = new List<(string?, string)>()
			{
				("image", string.Empty),
				("imgdesc", string.Empty),
				("race", string.Empty),
				("gender", npc.GenderText),
				("difficulty", npc.Difficulty > 0 ? npc.Difficulty.ToString(culture) : string.Empty),
				("reaction", npc.Reaction),
				("pickpocket", npc.PickpocketDifficulty > 0 ? npc.PickpocketDifficultyText : string.Empty),
				("loottype", npc.LootType),
				("faction", string.Empty)
			};

			var factory = new WikiNodeFactory();
			var template = factory.TemplateNodeFromParts("Online NPC Summary", true, parameters);
			UpdateLocations(npc, template, factory, placeInfo);

			return new StringBuilder()
				.Append("{{Minimal|NPC}}")
				.Append(WikiTextVisitor.Raw(template))
				.AppendLine()
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

		private static void UpdateLocations(NpcData npc, ITemplateNode template, IWikiNodeFactory factory, IEnumerable<PlaceInfo> places)
		{
			foreach (var (placeType, paramName, _, variesCount) in places)
			{
				var placeText = npc.GetParameterValue(placeType, variesCount);
				InsertLocation(template, factory, paramName, placeText);
			}

			if (npc.UnknownLocations.Count > 0)
			{
				var list = new SortedSet<string>(npc.UnknownLocations.Keys);
				var locText = string.Join(", ", list);
				InsertLocation(template, factory, "loc", locText);
			}

			static void InsertLocation(ITemplateNode template, IWikiNodeFactory factory, string name, string locText)
			{
				if (locText.Length > 0)
				{
					locText += '\n';
					if (template.Find(name) is IParameterNode loc)
					{
						loc.SetValue(loc.IsNullOrWhitespace() ? locText : (loc.Value.ToValue().TrimEnd() + ", " + locText));
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
		private NpcCollection FilterNpcs(NpcCollection npcs)
		{
			this.StatusWriteLine("Checking for existing pages");
			var retval = new NpcCollection();
			foreach (var npc in npcs)
			{
				ThrowNull(npc.Page, nameof(npc), nameof(npc.Page));
				if (npc.Page != null)
				{
					retval.Add(npc);
				}
			}

			return retval;
		}

		private NpcCollection GetNpcPages(PageCollection allPages)
		{
			var retval = EsoGeneral.GetNpcsFromDatabase();
			foreach (var npc in retval)
			{
				if (allPages.TryGetValue("Online:" + npc.Name, out var page))
				{
					string? issue = null;
					if (page.IsDisambiguation)
					{
						issue = "is a disambiguation with no clear NPC link";
						var parser = new ContextualParser(page);
						foreach (var linkNode in parser.LinkNodes)
						{
							var disambig = SiteLink.FromLinkNode(this.Site, linkNode, false);
							if (allPages.TryGetValue(disambig, out var disambigPage))
							{
								issue = null;
								npc.Page = disambigPage;
								break;
							}
						}
					}
					else if (allPages.TitleMap.TryGetValue(page.FullPageName, out var redirect))
					{
						if (allPages.TryGetValue(redirect, out var redirectPage))
						{
							npc.Page = redirectPage;
						}
						else
						{
							issue = "is a redirect to a content page without an Online NPC Summary";
						}
					}
					else
					{
						if (this.updateMode)
						{
							npc.Page = page;
						}
						else
						{
							issue = "is already a content page without an Online NPC Summary";
						}
					}

					if (issue != null)
					{
						// We use page instead of npc.Page for the link so we're not using redirected names.
						this.WriteLine($"* {page.AsLink(true)} {issue}. Please use the following data to create a page manually, if needed.\n*:Name: {npc.Name}\n*:Gender: {npc.GenderText}\n*:Loot Type: {npc.LootType}\n*:Known Locations: {string.Join(", ", npc.Places)}\n*:Unknown Locations: {string.Join(", ", npc.UnknownLocations)}");
					}
				}
			}

			return retval;
		}
		#endregion
	}
}