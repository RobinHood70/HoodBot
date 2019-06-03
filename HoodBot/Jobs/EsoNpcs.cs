namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Eso;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;

	internal class EsoNpcs : EditJob
	{
		#region Fields
		private readonly HashSet<string> places = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private PageCollection pages;
		#endregion

		#region Constructors
		[JobInfo("Create missing NPCs", "ESO")]
		public EsoNpcs(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Properties
		public override string LogName => "Create missing ESO NPC Pages";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.pages.Sort();
			this.StatusWriteLine("Saving");
			try
			{
				foreach (var page in this.pages)
				{
					try
					{
						page.Save("Create NPC page", true, Tristate.True, false);
					}
					catch (WikiException e) when (e.Code == "pagedeleted")
					{
						this.WriteLine($"* [[{page.FullPageName}|{page.LabelName}]] is blank, but has previously been deleted, so was not created again.");
					}

					this.Progress++;
				}
			}
			catch (EditConflictException)
			{
				// Do nothing. If someone created the page in the meantime, it's no longer our problem.
			}
		}

		protected override void PrepareJob()
		{
			this.Site.UserFunctions.SetResultTitle(ResultDestination.ResultsPage, "Existing ESO NPC pages");
			this.StatusWriteLine("Getting NPC data from wiki");
			var allNpcs = new TitleCollection(this.Site);
			allNpcs.GetCategoryMembers("Online-NPCs", CategoryMemberTypes.Page, false);
			allNpcs.GetCategoryMembers("Online-Creatures-All", CategoryMemberTypes.Page, false);

			this.CreatePages(allNpcs);

			this.ProgressMaximum = this.pages.Count + 1;
			this.Progress = 1;
		}
		#endregion

		#region Private Static Method
		private static string GetNPCHeader(NPCData npc) =>
			new Template("Online NPC Summary", true)
			{
				{ "image", string.Empty },
				{ "imgdesc", string.Empty },
				{ "race", string.Empty },
				{ "gender", npc.Gender.ToString() },
				{ "loc", string.Join(", ", npc.Locations) },
				{ "faction", string.Empty },
				{ "class", npc.Class }
			}.ToString();
		#endregion

		#region Private Methods

		private Page CreatePage(string name, NPCData npcData)
		{
			var sb = new StringBuilder();
			sb.Append("{{Minimal|NPC}}");
			sb.AppendLine(GetNPCHeader(npcData));
			sb.AppendLine("\n<!-- Instructions: Provide an initial sentence summarizing the NPC (race, job, where they live).  Subsequent paragraphs provide additional information about the NPC, such as related NPCs, schedule, equipment, etc.  Note that quest-specific information DOES NOT belong on this page, but instead goes on the appropriate quest page.  Spoilers should be avoided.-->");
			sb.AppendLine("{{NewLeft}}");
			sb.AppendLine("\n<!--Instructions: If this NPC is related to any quests, replace \"Quest Name\" with the quest's name.--><!--");
			sb.AppendLine("==Related Quests==\n*{{Quest Link|Quest Name}}");
			sb.AppendLine("--><!--Instructions: Add any miscellaneous notes about the NPC here, with a bullet for each note.--><!--");
			sb.AppendLine("==Notes==\n* Add note here");
			sb.AppendLine("--><!--Instructions: Add any bugs related to the NPC here using the format below.--><!--");
			sb.AppendLine("==Bugs==\n{{Bug|Bug description}}\n** Workaround\n-->");
			sb.AppendLine("\n{{Stub|NPC}}");

			var retval = new Page(this.Site, name) { Text = sb.ToString() };
			retval.SetMinimalStartTimestamp();

			return retval;
		}

		private void CreatePages(TitleCollection allNpcs)
		{
			this.StatusWriteLine("Getting NPC data from database");
			var unfilteredNpcList = EsoGeneral.GetNpcsFromDatabase();
			var filteredNpcList = this.FilterNpcList(allNpcs, unfilteredNpcList);
			var locQuery = $"SELECT DISTINCT npcId, zone FROM location USE INDEX (find_npcloc) WHERE npcId IN ({string.Join(",", filteredNpcList)})";
			this.GetLocationData(locQuery, filteredNpcList);
			this.GetPlacesData(filteredNpcList);

			var newNpcData = this.FilterNewNpcs(filteredNpcList);

			this.StatusWriteLine("Checking for existing pages");
			var checkPages = new PageCollection(this.Site, PageModules.Info | PageModules.Revisions | PageModules.Properties);
			checkPages.GetTitles(newNpcData.Keys);
			this.pages = new PageCollection(this.Site);
			foreach (var npc in newNpcData)
			{
				var page = checkPages[npc.Key];
				if (page.Exists)
				{
					var npcData = newNpcData[page.FullPageName];
					string issue = null;
					if (page.IsRedirect)
					{
						issue = "a redirect to a page without an Online NPC Summary";
						var redirectFinder = SiteLink.Find().Match(page.Text);
						SiteLink redirectTarget = null;
						if (redirectFinder.Success)
						{
							redirectTarget = new SiteLink(this.Site, redirectFinder.Value);
							if (allNpcs.Contains(redirectTarget))
							{
								issue = null;
							}
						}
					}
					else if (page.IsDisambiguation)
					{
						issue = "a disambiguation with no clear NPC link";
						var disambiguations = SiteLink.FindLinks(this.Site, page.Text, false);
						foreach (var disambig in disambiguations)
						{
							if (allNpcs.Contains(disambig))
							{
								issue = null;
								break;
							}
						}
					}
					else
					{
						issue = "already a content page without an Online NPC Summary";
					}

					if (issue != null)
					{
						var locations = npcData.Locations.Count >= 20 ? "suppressed due to excessive number" : string.Join(", ", npcData.Locations);
						this.WriteLine($"* [[{page.FullPageName}|{page.LabelName}]] is {issue}. Please use the following data to create a page manually, if needed.<br>Name: {npcData.Name}, Gender: {npcData.Gender}, Class: {npcData.Class}, Locations: {locations}");
					}
				}
				else
				{
					this.pages.Add(this.CreatePage(npc.Key, npc.Value));
				}
			}
		}

		private Dictionary<long, NPCData> FilterNpcList(TitleCollection allNpcs, Dictionary<long, NPCData> unfilteredNpcList)
		{
			var filteredNpcList = new Dictionary<long, NPCData>();
			foreach (var npc in unfilteredNpcList)
			{
				if (!allNpcs.Contains("Online:" + npc.Value.Name))
				{
					if (filteredNpcList.ContainsKey(npc.Key))
					{
						this.Warn($"Duplicate entry: {npc.Value.Name}");
					}
					else
					{
						filteredNpcList.Add(npc.Key, npc.Value);
					}
				}
			}

			return filteredNpcList;
		}

		private IReadOnlyDictionary<string, NPCData> FilterNewNpcs(Dictionary<long, NPCData> tempNpcData)
		{
			var newNpcData = new SortedDictionary<string, NPCData>();
			foreach (var entry in tempNpcData)
			{
				newNpcData.Add(this.Site.Namespaces[UespNamespaces.Online].DecoratedName + entry.Value.Name, entry.Value);
			}

			return newNpcData;
		}

		private void GetPlaces()
		{
			this.StatusWriteLine("Getting place data");
			var placeTitles = new TitleCollection(this.Site);
			placeTitles.GetCategoryMembers("Online-Places", false);
			foreach (var title in placeTitles)
			{
				this.places.Add(title.PageName);
			}
		}

		private void GetPlacesData(Dictionary<long, NPCData> tempNpcData)
		{
			this.GetPlaces();
			foreach (var npcEntry in tempNpcData)
			{
				var locs = npcEntry.Value.Locations;
				locs.Sort();
				for (var i = 0; i < locs.Count; i++)
				{
					if (this.places.Contains(locs[i]))
					{
						locs[i] = SiteLink.LinkTextFromParts(this.Site.Namespaces[UespNamespaces.Online], locs[i]);
					}
				}
			}
		}

		private void GetLocationData(string query, Dictionary<long, NPCData> tempNpcData)
		{
			this.StatusWriteLine("Getting location data");

			for (var retries = 0; retries < 3; retries++)
			{
				try
				{
					//// MySQL doesn't always play nice with the combination of DISTINCT and ORDER BY, so we use no sorting/uniqueness checks at all, then deal with both ourselves.
					foreach (var row in EsoGeneral.RunQuery(query))
					{
						var loc = (string)row["zone"];
						var npc = tempNpcData[(long)row["npcId"]];
						if (!npc.Locations.Contains(loc))
						{
							npc.Locations.Add(loc);
						}
					}

					break;
				}
				catch (Exception e)
				{
					Debug.WriteLine(e.GetType().FullName);
				}
			}
		}
		#endregion
	}
}