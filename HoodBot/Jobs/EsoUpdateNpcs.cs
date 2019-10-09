namespace RobinHood70.HoodBot.Jobs
{
	/*
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Eso;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;

	internal class EsoUpdateNpcs : EditJob
	{
		#region Fields
		private readonly Dictionary<string, NpcEntry> npcEntries = new Dictionary<string, NpcEntry>();
		private NpcCollection npcData;
		#endregion

		#region Constructors
		[JobInfo("Update NPC Info", "ESO")]
		public EsoUpdateNpcs(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Protected Override Properties
		public override string LogName => "Update NPC Info";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.npcData.SortByPageName();
			this.StatusWriteLine("Saving");
			foreach (var npc in this.npcData)
			{
				var page = npc.Page;
				if (page?.TextModified == false)
				{
					this.SavePage(page, "Update NPC info", false);
				}

				this.Progress++;
			}
		}

		protected override void PrepareJob()
		{
			this.StatusWriteLine("Getting NPC data from wiki");
			var allNpcs = EsoGeneral.GetNpcPages(this.Site);
			Debug.WriteLine(allNpcs.Count);

			this.StatusWriteLine("Getting NPC data from database");
			this.npcData = EsoGeneral.GetNpcsFromDatabase();
			var esoName = this.Site.Namespaces[UespNamespaces.Online].DecoratedName;
			foreach (var npc in this.npcData)
			{
				var pageName = esoName + npc.Name;
				if (!allNpcs.TryGetValue(pageName + " (npc)", out var page) && !allNpcs.TryGetValue(pageName + " (NPC)", out page))
				{
					allNpcs.TryGetValue(pageName, out page);
				}

				if (page != null)
				{
					if (page.Text == null)
					{
						this.WriteLine($"* Page load failed for {page.FullPageName}.");
					}
					else
					{
						npc.Page = page;
						var matches = Template.Find("Online NPC Summary").Matches(page.Text);
						if (matches.Count != 1)
						{
							this.WriteLine($"* [[{page.FullPageName}|]] not updated. More than one summary was found on the page.");
							continue;
						}

						var match = matches[0];
						var template = Template.Parse(match.Value);
						var locations = template["loc"];
						if (!Parameter.IsNullOrEmpty(locations))
						{
							npc.Locations.AddRange(locations.Value.Split(TextArrays.CommaSpace, StringSplitOptions.None));
						}

						this.npcEntries.Add(npc.Page.FullPageName, new NpcEntry(template, match.Index, match.Length));
					}
				}
			}

			this.ProgressMaximum = this.npcData.Count + 3;

			this.StatusWriteLine("Getting location data");
			var places = EsoGeneral.GetPlaces(this.Site);
			EsoGeneral.AddMissingLocations(this.npcData);
			EsoGeneral.FixUpLocations(this.npcData, places);
			this.Progress++;

			this.UpdatePages();
			this.Progress++;
		}
		#endregion

		#region Private Methods
		private void UpdatePages()
		{
			foreach (var npc in this.npcData)
			{
				var page = npc.Page;
				if (page != null && this.npcEntries.TryGetValue(page.FullPageName, out var entry))
				{
					var template = entry.Template;
					var oldText = template.ToString();
					if (npc.Difficulty > 0)
					{
						template.AddOrChange("difficulty", npc.Difficulty);
					}

					if (!template.Contains("creature"))
					{
						template.AddIfNotPresent("gender", npc.Gender.ToString());
					}

					template.AddIfBlank("loc", npc.LocationText);
					if (!string.IsNullOrEmpty(npc.LootType))
					{
						if (template["class"]?.Value == npc.LootType)
						{
							template.RenameParameter("class", "loottype");
						}
						else
						{
							template.AddOrChange("loottype", npc.LootType);
						}

						if (Parameter.IsNullOrEmpty(template["reaction"]))
						{
							template.Add("reaction", npc.Reaction);
						}
					}

					if (npc.PickpocketDifficulty > 0)
					{
						template.AddIfBlank("pickpocket", npc.PickpocketDifficulty.ToString());
					}

					var newText = template.ToString();
					if (oldText != newText)
					{
						template.Sort("image", "imgdesc", "titlename", "title", "lorepage", "city", "settlement", "loc", "house", "ship", "store", "race", "racecat", "creature", "gender", "health", "difficulty", "reaction", "class", "pickpocket", "loottype", "sells", "sells2", "other", "drops", "follower", "faction", "condition", "generic", "level", "soul", "notrail", "width");
					}

					page.Text = page.Text
						.Remove(entry.Index, entry.Length)
						.Insert(entry.Index, template.ToString());
				}
			}
		}
		#endregion

		#region Private Classes
		private class NpcEntry
		{
			#region Constructors
			public NpcEntry(Template template, int index, int length)
			{
				this.Template = template;
				this.Index = index;
				this.Length = length;
			}
			#endregion

			#region Public Properties
			public int Index { get; }

			public int Length { get; }

			public Template Template { get; }
			#endregion
		}
		#endregion
	}

	*/
}
