namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;

	internal class EsoNpcs : EditJob
	{
		#region Constructors
		[JobInfo("Create missing NPCs", "ESO")]
		public EsoNpcs(Site site, AsyncInfo asyncInfo)
				: base(site, asyncInfo) => this.SetResultDescription("Existing ESO NPC pages");
		#endregion

		#region Protected Override Properties
		public override string LogName => "Create Missing ESO NPC Pages";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Getting NPC data from wiki");
			var allNpcs = EsoGeneral.GetNpcsFromCategories(this.Site);

			this.StatusWriteLine("Getting NPC data from database");
			var npcData = EsoGeneral.GetNpcsFromDatabase();
			this.FilterNpcs(allNpcs, npcData);
			EsoGeneral.GetNpcLocations(npcData);

			this.StatusWriteLine("Getting place data from wiki");
			var places = EsoGeneral.GetPlaces(this.Site);
			EsoGeneral.ParseNpcLocations(npcData, places);

			foreach (var npc in npcData)
			{
				npc.TrimPlaces();
				this.Pages.Add(this.CreatePage(npc));
			}

			this.ProgressMaximum = this.Pages.Count + 4;
			this.Progress = 4;
		}

		protected override void Main()
		{
			this.StatusWriteLine("Saving");
			try
			{
				foreach (var page in this.Pages)
				{
					try
					{
						page.Save(this.LogName, false, Tristate.True, false);
					}
					catch (WikiException e) when (e.Code == "pagedeleted")
					{
						this.WriteLine($"* [[{page.FullPageName}|]] is blank, but has previously been deleted, so was not created again.");
					}

					this.Progress++;
				}
			}
			catch (EditConflictException)
			{
				// Do nothing. If someone created the page in the meantime, it's no longer our problem.
			}
		}
		#endregion

		#region Private Static Methods
		private Page CreatePage(NpcData npc)
		{
			var template = new Template("Online NPC Summary", true)
			{
				{ "image", string.Empty },
				{ "imgdesc", string.Empty },
				{ "race", string.Empty },
				{ "gender", npc.Gender.ToString() },
				{ "difficulty", npc.Difficulty > 0 ? npc.Difficulty.ToString() : string.Empty },
				{ "reaction", npc.Reaction },
				{ "pickpocket", npc.PickpocketDifficulty > 0 ? npc.PickpocketDifficulty.ToString() : string.Empty },
				{ "loottype", npc.LootType },
				{ "faction", string.Empty },
			};

			foreach (var (placeType, paramName, _, variesCount) in EsoGeneral.PlaceInfo)
			{
				var placeText = npc.GetParameterValue(placeType, variesCount);
				if (placeText.Length > 0)
				{
					template.AddBefore("race", paramName, placeText);
				}
			}

			if (npc.UnknownLocations.Count > 0)
			{
				var list = new SortedSet<string>(npc.UnknownLocations.Keys);
				var locText = string.Join(", ", list);
				var loc = template["loc"];
				if (loc != null)
				{
					loc.Value += ", " + locText;
				}
				else
				{
					template.AddBefore("race", "loc", locText);
				}
			}

			var text = new StringBuilder()
				.Append("{{Minimal|NPC}}")
				.AppendLine(template.ToString())
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

			var page = new Page(this.Site, UespNamespaces.Online, npc.PageName) { Text = text };
			page.SetMinimalStartTimestamp();
			return page;
		}
		#endregion

		#region Private Methods
		private void FilterNpcs(TitleCollection allNpcs, NpcCollection npcData)
		{
			this.StatusWriteLine("Checking for existing pages");
			var tempList = new NpcCollection();
			var titlesOnly = new TitleCollection(this.Site);
			foreach (var npc in npcData)
			{
				var title = new NpcTitle(this.Site, npc);
				if (allNpcs.ValueOrDefault(title) is null)
				{
					titlesOnly.Add(title);
				}
			}

			titlesOnly.Sort();

			var pageLoadOptions = new PageLoadOptions(PageModules.Default | PageModules.Properties, true);
			var checkPages = titlesOnly.Load(pageLoadOptions);
			//// checkPages.Sort();
			foreach (var title in titlesOnly)
			{
				var npc = ((NpcTitle)title).Npc;
				var page = checkPages[title.FullPageName];
				if (page.Exists)
				{
					string? issue;
					if (page.IsDisambiguation)
					{
						issue = "a disambiguation with no clear NPC link";
						var parser = WikiTextParser.Parse(page.Text);
						foreach (var linkNode in parser.FindAllRecursive<LinkNode>())
						{
							var disambig = SiteLink.FromLinkNode(this.Site, linkNode, false);
							if (allNpcs.Contains(disambig))
							{
								issue = null;
								npc.PageName = disambig.PageName;
								break;
							}
						}
					}
					else if (checkPages.TitleMap.TryGetValue(title.FullPageName, out var redirect))
					{
						if (allNpcs.Contains(redirect.FullPageName))
						{
							issue = null;
							npc.PageName = redirect.PageName;
						}
						else
						{
							issue = "a redirect to a content page without an Online NPC Summary";
						}
					}
					else
					{
						issue = "already a content page without an Online NPC Summary";
					}

					if (issue != null)
					{
						this.WriteLine($"* [[{page.FullPageName}|{page.LabelName}]] is {issue}. Please use the following data to create a page manually, if needed.\n*:Name: {npc.Name}\n*:Gender: {npc.Gender}\n*:Loot Type: {npc.LootType}\n*:Known Locations: {string.Join(", ", npc.Places)}\n*:Unknown Locations: {string.Join(", ", npc.UnknownLocations)}");
					}
				}
				else
				{
					tempList.Add(npc);
				}
			}

			npcData.Clear();
			npcData.AddRange(tempList);
		}
		#endregion

		#region Private Classes
		private class NpcTitle : Title
		{
			public NpcTitle(Site site, NpcData npc)
				: base(site, UespNamespaces.Online, npc.Name, true) => this.Npc = npc;

			public NpcData Npc { get; }
		}
		#endregion
	}
}