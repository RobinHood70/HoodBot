namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using MySql.Data.MySqlClient;
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
		private readonly HashSet<string> overrides = new HashSet<string>()
		{
			"Online:Veeskhleel Shadecaller",
			"Online:Veeskhleel Predator",
			"Online:Vadeshta",
			"Online:Udazada",
			"Online:Tormented Vestige",
			"Online:Sliding Stone",
			"Online:Sharzahir",
			"Online:Shagrath's Host",
			"Online:Severine Leonciele",
			"Online:Sargent Themond",
			"Online:Sargent Lort",
			"Online:Sangiin's Thirst",
			"Online:Sacrificial Helot",
			"Online:Runs-In-Wild",
			"Online:Nebzezir",
			"Online:Moon-Priest Haduras",
			"Online:Moongrave Sentinel",
			"Online:Luahna",
			"Online:Kujo Kethba",
			"Online:Keshazh",
			"Online:Hollowfang Skullguard",
			"Online:Hollowfang Dire-Maw",
			"Online:Hollowfang Bloodpanther",
			"Online:Hemo Helot",
			"Online:Grapple Point",
			"Online:Fledgeling Gryphon",
			"Online:Firhesan",
			"Online:Eternal Servant",
			"Online:Enzamir",
			"Online:Dro'zakar",
			"Online:Dhulavir",
			"Online:Danouida",
			"Online:Colby Rangouze",
			"Online:Coagulant",
			"Online:Azureblight Seed",
		};

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
						page.Save("Update missing location(s)", true, Tristate.Unknown, false);
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
		private static EsoNpcList FilterNpcList(TitleCollection allNpcs, EsoNpcList npcList)
		{
			var retval = new EsoNpcList();
			foreach (var npc in npcList)
			{
				if (!allNpcs.Contains("Online:" + npc.Name))
				{
					retval.Add(npc);
				}
			}

			return retval;
		}

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

		private Page CreatePage(NPCData npc)
		{
			var sb = new StringBuilder();
			sb.Append("{{Minimal|NPC}}");
			sb.AppendLine(GetNPCHeader(npc));
			sb.AppendLine("\n<!-- Instructions: Provide an initial sentence summarizing the NPC (race, job, where they live).  Subsequent paragraphs provide additional information about the NPC, such as related NPCs, schedule, equipment, etc.  Note that quest-specific information DOES NOT belong on this page, but instead goes on the appropriate quest page.  Spoilers should be avoided.-->");
			sb.AppendLine("{{NewLeft}}");
			sb.AppendLine("\n<!--Instructions: If this NPC is related to any quests, replace \"Quest Name\" with the quest's name.--><!--");
			sb.AppendLine("==Related Quests==\n*{{Quest Link|Quest Name}}");
			sb.AppendLine("--><!--Instructions: Add any miscellaneous notes about the NPC here, with a bullet for each note.--><!--");
			sb.AppendLine("==Notes==\n* Add note here");
			sb.AppendLine("--><!--Instructions: Add any bugs related to the NPC here using the format below.--><!--");
			sb.AppendLine("==Bugs==\n{{Bug|Bug description}}\n** Workaround\n-->");
			sb.AppendLine("\n{{Stub|NPC}}");

			var retval = new Page(this.Site, npc.PageName) { Text = sb.ToString() };
			// retval.SetMinimalStartTimestamp();

			return retval;
		}

		private void CreatePages(TitleCollection allNpcs)
		{
			this.pages = new PageCollection(this.Site);
			foreach (var page in this.overrides)
			{
				allNpcs.Remove(page);
			}

			this.StatusWriteLine("Getting NPC data from database");
			var npcList = EsoGeneral.GetNpcsFromDatabase(this.Site.Namespaces[UespNamespaces.Online]);
			npcList = FilterNpcList(allNpcs, npcList);
			if (npcList.Count == 0)
			{
				return;
			}

			npcList.SortByPageName();

			this.StatusWriteLine("Checking for existing pages");
			var titlesOnly = new TitleCollection(this.Site);
			foreach (var npc in npcList)
			{
				titlesOnly.Add(npc.PageName);
			}

			var checkPages = titlesOnly.Load(PageModules.Info | PageModules.Revisions | PageModules.Properties);
			var pagesToSave = new EsoNpcList();
			foreach (var npc in npcList)
			{
				var page = checkPages[npc.PageName];
				if (page.Exists && !this.overrides.Contains(page.FullPageName))
				{
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
						var locations = npc.Locations.Count >= 20 ? "suppressed due to excessive number" : string.Join(", ", npc.Locations);
						this.WriteLine($"* [[{page.FullPageName}|{page.LabelName}]] is {issue}. Please use the following data to create a page manually, if needed.<br>Name: {npc.Name}, Gender: {npc.Gender}, Class: {npc.Class}, Locations: {locations}");
					}
				}
				else
				{
					pagesToSave.Add(npc);
				}
			}

			this.GetLocationData(pagesToSave);
			this.GetPlacesData(pagesToSave);
			foreach (var npc in pagesToSave)
			{
				this.pages.Add(this.CreatePage(npc));
			}
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

		private void GetPlacesData(EsoNpcList npcList)
		{
			this.GetPlaces();
			foreach (var npcEntry in npcList)
			{
				var locs = npcEntry.Locations;
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

		private void GetLocationData(EsoNpcList npcList)
		{
			this.StatusWriteLine("Getting location data");

			//// MySQL doesn't do well with an IN() clause and we don't have CREATE TEMPORARY TABLE permissions, so work around these two issues.
			var filter = new StringBuilder();
			foreach (var npc in npcList)
			{
				filter.Append(" OR npcId=" + npc.Id.ToStringInvariant());
			}

			filter = filter.Remove(0, 4);
			var query = $"SELECT DISTINCT npcId, zone FROM location USE INDEX (find_npcloc) WHERE " + filter.ToString();
			for (var retries = 0; retries < 4; retries++)
			{
				try
				{
					foreach (var row in EsoGeneral.RunQuery(query))
					{
						var loc = (string)row["zone"];
						var npc = npcList[(long)row["npcId"]];
						if (!npc.Locations.Contains(loc))
						{
							npc.Locations.Add(loc);
						}
					}

					break;
				}
				catch (MySqlException)
				{
					if (retries == 3)
					{
						throw;
					}
				}
			}
		}
		#endregion
	}
}