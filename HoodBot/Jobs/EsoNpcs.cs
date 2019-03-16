namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;

	internal class EsoNpcs : EditJob
	{
		#region Fields
		private PageCollection pages;
		#endregion

		#region Constructors
		[JobInfo("Create missing NPCs", "ESO")]
		public EsoNpcs(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => site.EditingDisabled = true;
		#endregion

		#region Private Enumerations
		private enum Gender
		{
			Unknown = 0,
			Female = 1,
			Male = 2
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
					page.Save("Create NPC page", true, Tristate.True, true);
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
			this.StatusWriteLine("Getting wiki data");
			var newNpcs = new TitleCollection(this.Site);
			newNpcs.GetCategoryMembers("Online-NPCs", false);
			newNpcs.GetCategoryMembers("Online-Creatures-All", false);
			var newNpcData = this.FilterNewNpcs(newNpcs);

			this.StatusWriteLine("Checking for existing pages");
			var checkPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties);
			checkPages.GetTitles(newNpcData.Keys);
			this.pages = new PageCollection(this.Site);
			foreach (var npc in newNpcData)
			{
				var page = checkPages[npc.Key];
				if (page.Exists)
				{
					newNpcs.Remove(page);
					this.Write($"* [[{page.FullPageName}|{page.LabelName}]] is ");
					if (page.IsRedirect)
					{
						this.WriteLine("a redirect.");
					}
					else if (page.IsDisambiguation)
					{
						this.WriteLine("a disambiguation page.");
					}
					else
					{
						this.WriteLine("already a content page.");
					}

					var npcData = newNpcData[page.FullPageName];
					this.WriteLine($"<br>Please use the following data to create a page manually - Name: {npcData.Name}, Gender: {npcData.Gender}, Class: {npcData.Class}, Locations: {npcData.Locations}");
				}
				else
				{
					this.pages.Add(this.CreatePage(npc.Key, npc.Value));
				}
			}

			this.ProgressMaximum = this.pages.Count + 1;
			this.Progress = 1;
		}
		#endregion

		#region Private Static Method
		private static string GetNPCHeader(NPCData npc)
		{
			var npcSummary = new Template("Online NPC Summary")
			{
				{ "image", string.Empty },
				{ "imgdesc", string.Empty },
				{ "race", string.Empty },
				{ "gender", npc.Gender.ToString() },
				{ "loc", string.Join(", ", npc.Locations) },
				{ "faction", string.Empty },
				{ "class", npc.Class }
			};

			npcSummary.NameParameter.TrailingWhiteSpace = "\n";
			npcSummary.DefaultValueFormat.TrailingWhiteSpace = "\n";

			return npcSummary.ToString();
		}
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

			return new Page(this.Site, name) { Text = sb.ToString() };
		}

		private Dictionary<string, NPCData> FilterNewNpcs(TitleCollection allNPCs)
		{
			var tempNpcData = this.MergeNpcData(allNPCs);
			var newNpcData = new Dictionary<string, NPCData>();
			foreach (var entry in tempNpcData)
			{
				newNpcData.Add(this.Site.Namespaces[UespNamespaces.Online].DecoratedName + entry.Value.Name, entry.Value);
			}

			return newNpcData;
		}

		private HashSet<string> GetPlaces()
		{
			this.StatusWriteLine("Getting place data");
			var places = new TitleCollection(this.Site);
			places.GetCategoryMembers("Online-Places", false);
			var list = new HashSet<string>();
			foreach (var place in places)
			{
				list.Add(place.PageName);
			}

			return list;
		}

		private Dictionary<long, NPCData> MergeNpcData(TitleCollection allNPCs)
		{
			var places = this.GetPlaces();

			this.StatusWriteLine("Getting NPC data");
			var limit = new SortedSet<long>();
			var tempNpcData = new Dictionary<long, NPCData>();
			foreach (var row in Eso.EsoGeneral.RunEsoQuery("SELECT id, name, gender, ppClass FROM uesp_esolog.npc WHERE level != -1"))
			{
				var name = ((string)row["name"]).TrimEnd(); // Corrects a single record where the field has a tab at the end of it - seems to be an ESO problem
				var removed = allNPCs.Remove("Online:" + name);
				if (!removed)
				{
					var id = (long)row["id"];
					var npcData = new NPCData(name, (sbyte)row["gender"], (string)row["ppClass"]);
					if (limit.Contains(id))
					{
						this.Warn($"Duplicate entry: {npcData.Name}");
					}
					else
					{
						tempNpcData.Add(id, npcData);
						limit.Add(id);
					}
				}
			}

			this.StatusWriteLine("Getting location data");

			// MySQL doesn't always play nice with the combination of DISTINCT and ORDER BY, so we use DISTINCT only, then sort the results ourselves.
			foreach (var row in Eso.EsoGeneral.RunEsoQuery($"SELECT DISTINCT npcId, zone FROM location WHERE npcId IN ({string.Join(",", limit)})"))
			{
				var loc = (string)row["zone"];
				var npc = tempNpcData[(long)row["npcId"]];
				npc.Locations.Add(loc);
			}

			foreach (var npcEntry in tempNpcData)
			{
				var oldLocs = npcEntry.Value.Locations;
				var newLocs = new List<string>(oldLocs);
				newLocs.Sort();
				for (var i = 0; i < newLocs.Count; i++)
				{
					if (places.Contains(newLocs[i]))
					{
						newLocs[i] = Title.NameFromParts(this.Site.Namespaces[UespNamespaces.Online], newLocs[i]);
					}
				}

				oldLocs.Clear();
				oldLocs.AddRange(newLocs);
			}

			return tempNpcData;
		}
		#endregion

		#region Private Classes
		private class NPCData
		{
			#region Constructors
			public NPCData(string name, sbyte gender, string npcClass)
			{
				this.Name = name;
				this.Gender = (Gender)gender;
				this.Class = npcClass;
			}
			#endregion

			#region Public Properties
			public string Class { get; }

			public Gender Gender { get; }

			public List<string> Locations { get; } = new List<string>();

			public string Name { get; }
			#endregion

			#region Public Override Methods
			public override string ToString() => this.Name;
			#endregion
		}
		#endregion
	}
}