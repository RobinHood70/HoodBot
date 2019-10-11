namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using MySql.Data.MySqlClient;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	#region Public Enumerations
	public enum Gender
	{
		Unknown = 0,
		Female = 1,
		Male = 2
	}
	#endregion

	internal static class EsoGeneral
	{
		#region Private Constants
		private const string PatchPageName = "Online:Patch";
		#endregion

		#region Static Fields
		private static readonly Regex BonusFinder = new Regex(@"\s*Current [Bb]onus:.*?\.");
		private static readonly Regex SpaceFixer = new Regex(@"[\n\ ]+");
		private static readonly string EsoLogConnectionString = ConfigurationManager.ConnectionStrings["EsoLog"].ConnectionString;
		private static string patchVersion = null;
		#endregion

		#region Public Properties
		public static Dictionary<int, string> MechanicNames { get; } = new Dictionary<int, string>
		{
			[-2] = "Health",
			[0] = "Magicka",
			[6] = "Stamina",
			[10] = "Ultimate",
			[-50] = "Ultimate (no weapon damage)",
			[-51] = "Light Armor #",
			[-52] = "Medium Armor #",
			[-53] = "Heavy Armor #",
			[-54] = "Dagger #",
			[-55] = "Armor Type #",
			[-56] = "Spell + Weapon Damage",
			[-57] = "Assassination Skills Slotted",
			[-58] = "Fighters Guild Skills Slotted",
			[-59] = "Draconic Power Skills Slotted",
			[-60] = "Shadow Skills Slotted",
			[-61] = "Siphoning Skills Slotted",
			[-62] = "Sorcerer Skills Slotted",
			[-63] = "Mages Guild Skills Slotted",
			[-64] = "Support Skills Slotted",
			[-65] = "Animal Companion Skills Slotted",
			[-66] = "Green Balance Skills Slotted",
			[-67] = "Winter's Embrace Slotted",
			[-68] = "Magicka with Health Cap",
			[-69] = "Magicka with Health Cap",
		};

		public static IEnumerable<PlaceInfo> PlaceInfo { get; } = new PlaceInfo[]
		{
			new PlaceInfo(PlaceType.City, "city", "Online-Places-Cities", 5),
			new PlaceInfo(PlaceType.Settlement, "settlement", "Online-Places-Settlements", 5),
			new PlaceInfo(PlaceType.Unknown, "loc", null, 10),
			new PlaceInfo(PlaceType.House, "house", "Online-Places-Homes", 1),
			new PlaceInfo(PlaceType.Ship, "ship", "Online-Places-Ships", 1),
			new PlaceInfo(PlaceType.Store, "store", "Online-Places-Stores", 1),
		};
		#endregion

		#region Public Methods
		public static void GetNpcLocations(NpcCollection npcData)
		{
			var npcIds = new List<long>(npcData.Count);
			foreach (var npc in npcData)
			{
				if (npc.AllLocations.Count == 0)
				{
					npcIds.Add(npc.Id);
				}
			}

			if (npcIds.Count == 0)
			{
				return;
			}

			var query = $"SELECT npcId, zone, locCount FROM npcLocations WHERE npcId IN ({string.Join(", ", npcIds)}) AND zone != 'Tamriel'";
			for (var retries = 0; retries < 4; retries++)
			{
				try
				{
					foreach (var row in RunQuery(query))
					{
						var npcId = (long)row["npcId"];
						var location = (string)row["zone"];
						var count = (int)row["locCount"];
						var npc = npcData[npcId];
						npc.AllLocations.Add(location, count);
					}

					break;
				}
				catch (TimeoutException)
				{
					if (retries == 3)
					{
						throw;
					}
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

		public static TitleCollection GetNpcsFromCategories(Site site)
		{
			var retval = new TitleCollection(site);
			retval.GetCategoryMembers("Online-NPCs", CategoryMemberTypes.Page, false);
			retval.GetCategoryMembers("Online-Creatures-All", CategoryMemberTypes.Page, false);

			return retval;
		}

		public static NpcCollection GetNpcsFromDatabase()
		{
			var retval = new NpcCollection();
			var nameClash = new HashSet<string>();
			foreach (var row in RunQuery("SELECT id, name, gender, difficulty, ppDifficulty, ppClass FROM uesp_esolog.npc WHERE level != -1"))
			{
				var npcData = new NpcData(row);
				if (!ReplacementData.NpcNameSkips.Contains(npcData.Name))
				{
					if (!nameClash.Add(npcData.Name))
					{
						throw new InvalidOperationException($"Warning: an NPC with the name \"{npcData.Name}\" exists more than once in the database!");
					}

					retval.Add(npcData);
				}
			}

			return retval;
		}

		public static PageCollection GetNpcPages(Site site)
		{
			var retval = new PageCollection(site);
			retval.SetLimitations(LimitationType.FilterTo, UespNamespaces.Online);
			retval.GetPageTranscludedIn(new[] { new Title(site.Namespaces[UespNamespaces.Template], "Online NPC Summary") });
			retval.Sort();

			return retval;
		}

		public static string GetPatchVersion(WikiJob job)
		{
			if (patchVersion == null)
			{
				job.StatusWriteLine("Fetching ESO update number");
				var patchTitle = new TitleCollection(job.Site, PatchPageName);
				var pageLoadOptions = new PageLoadOptions(PageModules.Custom);
				var pageCreator = (job.Site.PageCreator as MetaTemplateCreator) ?? new MetaTemplateCreator();
				var patchPage = patchTitle.Load(pageLoadOptions, pageCreator)[0] as VariablesPage;
				patchVersion = patchPage.MainSet["number"];
			}

			return patchVersion;
		}

		public static PlaceCollection GetPlaces(Site site)
		{
			var pageLoadOptions = new PageLoadOptions(PageModules.Custom, true);
			var pageCreator = new MetaTemplateCreator();
			pageCreator.VariableNames.AddRange("alliance", "settlement", "titlename", "zone");
			var places = new PageCollection(site, pageLoadOptions, pageCreator);
			places.GetCategoryMembers("Online-Places", false);

			var retval = new PlaceCollection();
			foreach (VariablesPage page in places)
			{
				if (page.MainSet != null)
				{
					retval.Add(new Place(page));
				}
			}

			foreach (var mappedName in places.TitleMap)
			{
				if (retval.TryGetValue(mappedName.Value.PageName, out var place))
				{
					// In an ideal world, this would be a direct reference to the same place, rather than a copy, but that ends up being a lot of work for very little gain.
					var key = new Title(site, mappedName.Key).PageName;
					retval.Add(Place.Copy(key, place));
				}
			}

			foreach (var placeInfo in PlaceInfo)
			{
				if (placeInfo.CategoryName != null)
				{
					GetPlaceCategory(site, retval, placeInfo);
				}
			}

			return retval;
		}

		public static string HarmonizeDescription(string desc) => SpaceFixer.Replace(BonusFinder.Replace(desc, string.Empty), " ");

		public static void ParseNpcLocations(NpcCollection npcData, PlaceCollection places)
		{
			foreach (var npc in npcData)
			{
				var locCopy = new Dictionary<string, int>(npc.AllLocations);
				foreach (var kvp in locCopy)
				{
					var key = kvp.Key;
					if (places.TryGetValue(key, out var place))
					{
						npc.AllPlaces.Add(place, kvp.Value);
						npc.AllLocations.Remove(key);
					}
					else
					{
						Debug.WriteLine($"Location not found: {key}");
					}
				}
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "No user input.")]
		public static IEnumerable<IDataRecord> RunQuery(string query)
		{
			using var connection = new MySqlConnection(EsoLogConnectionString);
			connection.Open();
			using var command = new MySqlCommand(query, connection);
			using var reader = command.ExecuteReader();
			while (reader.Read())
			{
				yield return reader;
			}
		}

		public static void SetBotUpdateVersion(WikiJob job, string pageType)
		{
			// Assumes EsoPatchVersion has already been updated.
			ThrowNull(pageType, nameof(pageType));
			job.StatusWriteLine("Update patch bot parameters");
			var paramName = "bot" + pageType;
			var patchPage = new TitleCollection(job.Site, PatchPageName).Load()[0];
			var match = Template.Find("Online Patch").Match(patchPage.Text);
			var patchTemplate = Template.Parse(match.Value);
			var oldValue = patchTemplate[paramName]?.Value;
			var patchVersion = GetPatchVersion(job);
			if (oldValue != patchVersion)
			{
				patchTemplate.AddOrChange(paramName, patchVersion);
				patchPage.Text = patchPage.Text
					.Remove(match.Index, match.Length)
					.Insert(match.Index, patchTemplate.ToString());

				patchPage.Save("Update " + paramName, true);
			}
		}

		public static string TimeToText(int time) => ((double)time).ToString("0,.#");
		#endregion

		#region Private Methods
		private static void GetPlaceCategory(Site site, PlaceCollection places, PlaceInfo placeInfo)
		{
			var cat = new PageCollection(site);
			cat.GetCategoryMembers(placeInfo.CategoryName, false);
			foreach (var member in cat)
			{
				if (member.Namespace == UespNamespaces.Online)
				{
					if (places.TryGetValue(member.PageName, out var place))
					{
						if (place.Type == PlaceType.Unknown)
						{
							place.Type = placeInfo.Type;
						}
						else
						{
							Debug.WriteLine($"Multiple place types on page: {member.FullPageName}");
						}
					}
				}
				else if (member.Namespace != UespNamespaces.Category)
				{
					Debug.WriteLine($"Unexpected page [[{member.FullPageName}]] found in [[:Category:{placeInfo.CategoryName}]].");
				}
			}
		}
		#endregion
	}
}