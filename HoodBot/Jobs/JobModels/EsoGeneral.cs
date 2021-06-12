namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using MySql.Data.MySqlClient;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	#region Public Enumerations
	public enum Gender
	{
		None = -1,
		NotApplicable = 0,
		Female = 1,
		Male = 2
	}
	#endregion

	internal static class EsoGeneral
	{
		#region Static Fields
		private static readonly Regex ColourCode = new(@"\A\|c[0-9A-F]{6}(.*?)\|r\Z", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex TrailingDigits = new(@"\s*\d+\Z", RegexOptions.None, DefaultRegexTimeout);
		#endregion

		#region Fields
		private static readonly Regex BonusFinder = new(@"\s*Current [Bb]onus:.*?\.", RegexOptions.None, DefaultRegexTimeout);
		private static string? patchVersion;
		#endregion

		#region Public Properties
		public static string EsoLogConnectionString { get; } = App.GetConnectionString("EsoLog");

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
			[-69] = "Bone Tyrant Slotted",
			[-70] = "Grave Lord Slotted",
			[-71] = "Spell Damage Capped",
			[-72] = "Magicka and Weapon Damage",
			[-73] = "Magicka and Spell Damage",
		};

		public static IReadOnlyList<PlaceInfo> PlaceInfo { get; } = new PlaceInfo[]
		{
			new PlaceInfo(PlaceType.City, "city", "Online-Places-Cities", 5),
			new PlaceInfo(PlaceType.Settlement, "settlement", "Online-Places-Settlements", 5),
			new PlaceInfo(PlaceType.House, "house", "Online-Places-Homes", 1),
			new PlaceInfo(PlaceType.Ship, "ship", "Online-Places-Ships", 1),
			new PlaceInfo(PlaceType.Store, "store", "Online-Places-Stores", 1),
			new PlaceInfo(PlaceType.Unknown, "loc", null, 10),
		};
		#endregion

		#region Public Methods
		public static void GetNpcLocations(NpcCollection npcData)
		{
			var npcIds = new List<long>(npcData.Count);
			foreach (var npc in npcData)
			{
				if (npc.UnknownLocations.Count == 0)
				{
					npcIds.Add(npc.Id);
				}
			}

			if (npcIds.Count == 0)
			{
				return;
			}

			var query = $"SELECT npcId, zone, locCount FROM npcLocations WHERE npcId IN ({string.Join(", ", npcIds)}) AND zone != 'Tamriel'";
			for (var retries = 0; true; retries++)
			{
				try
				{
					foreach (var row in Database.RunQuery(EsoLogConnectionString, query))
					{
						var npcId = (long)row["npcId"];
						var location = (string)row["zone"];
						var count = (int)row["locCount"];
						var npc = npcData[npcId];
						var place = new Place(location);
						npc.UnknownLocations.Add(place, count);
					}

					break;
				}
				catch (TimeoutException) when (retries < 3)
				{
				}
				catch (MySqlException) when (retries < 3)
				{
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
			// Note: for now, it's assumed that the collection should be the same across all jobs, so all filtering is done here in the query (e.g., Reaction != 6 for companions). If this becomes untrue at some point, filtering will have to be shifted to the individual jobs or we could add a query string to the call.
			var retval = new NpcCollection();
			var nameClash = new HashSet<string>(StringComparer.Ordinal);
			var throwNameClash = false;
			foreach (var row in Database.RunQuery(EsoLogConnectionString, "SELECT id, name, gender, difficulty, ppDifficulty, ppClass, reaction FROM uesp_esolog.npc WHERE level != -1 AND reaction != 6"))
			{
				var name = (string)row["name"];
				if (!ColourCode.IsMatch(name) && !TrailingDigits.IsMatch(name))
				{
					var npcData = new NpcData(row);
					if (!ReplacementData.NpcNameSkips.Contains(npcData.Name))
					{
						if (nameClash.Add(npcData.Name))
						{
							retval.Add(npcData);
						}
						else
						{
							Debug.WriteLine($"Warning: an NPC with the name \"{npcData.Name}\" exists more than once in the database!");
							throwNameClash = true;
						}
					}
				}
			}

			if (throwNameClash)
			{
				throw new InvalidOperationException("Duplicate NPCs found. Operation aborted! See debug output for specifics.");
			}

			return retval;
		}

		public static PageCollection GetNpcPages(Site site)
		{
			var retval = new PageCollection(site);
			retval.SetLimitations(LimitationType.FilterTo, UespNamespaces.Online);
			retval.GetPageTranscludedIn(new[] { new Title(site[UespNamespaces.Template], "Online NPC Summary") });
			retval.Sort();

			return retval;
		}

		public static VariablesPage GetPatchPage(WikiJob job)
		{
			job.StatusWriteLine("Fetching ESO update number");
			var patchTitle = new TitleCollection(job.Site, "Online:Patch");
			var pages = job.Site.CreateMetaPageCollection(PageModules.Default, false);
			pages.GetTitles(patchTitle);
			if (pages.Count == 1
				&& pages[0] is VariablesPage patchPage
				&& patchPage.MainSet?["number"] is string version)
			{
				patchVersion = version;
				return patchPage;
			}

			throw new InvalidOperationException("Could not find patch version on page.");
		}

		public static string GetPatchVersion(WikiJob job)
		{
			if (patchVersion == null)
			{
				GetPatchPage(job);
			}

			return patchVersion!;
		}

		public static PlaceCollection GetPlaces(Site site)
		{
			ThrowNull(site, nameof(site));
			var places = site.CreateMetaPageCollection(PageModules.None, true, "alliance", "settlement", "titlename", "type", "zone");
			places.SetLimitations(LimitationType.FilterTo, UespNamespaces.Online);
			places.GetCategoryMembers("Online-Places");

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
				// TODO: Take another look at this later. Error catching added here that triggered on [[Online:Hircine's Hunting Grounds]]. Having a bad day and not sure if this is the right thing to do.
				try
				{
					if (retval[mappedName.Value.PageName] is Place place)
					{
						// In an ideal world, this would be a direct reference to the same place, rather than a copy, but that ends up being a lot of work for very little gain.
						var key = Title.FromName(site, mappedName.Key).PageName;
						retval.Add(Place.Copy(key, place));
					}
				}
				catch (InvalidOperationException)
				{
				}
			}

			foreach (var placeInfo in PlaceInfo)
			{
				GetPlaceCategory(site, retval, placeInfo);
			}

			return retval;
		}

		public static string HarmonizeDescription(string desc) => RegexLibrary.WhitespaceToSpace(BonusFinder.Replace(desc, string.Empty));

		public static void ParseNpcLocations(NpcCollection npcData, PlaceCollection places)
		{
			foreach (var npc in npcData)
			{
				if (npc.Page != null)
				{
					var locCopy = new Dictionary<Place, int>(npc.UnknownLocations);
					foreach (var kvp in locCopy)
					{
						var key = kvp.Key;
						try
						{
							if (places[key.TitleName] is Place place)
							{
								npc.Places.Add(place, kvp.Value);
								npc.UnknownLocations.Remove(key);
							}
							else
							{
								Debug.WriteLine($"Location not found: {key}");
							}
						}
						catch (InvalidOperationException)
						{
							Debug.WriteLine($"Location {key.TitleName} is ambiguous for NPC {npc.Name}");
						}
					}
				}
			}
		}

		public static void SetBotUpdateVersion(WikiJob job, string pageType)
		{
			// Assumes EsoPatchVersion has already been updated.
			ThrowNull(pageType, nameof(pageType));
			job.StatusWriteLine("Update patch bot parameters");

			var patchPage = GetPatchPage(job);
			var parser = new ContextualParser(patchPage);
			var paramName = "bot" + pageType;
			if (parser.FindTemplate("Online Patch") is ITemplateNode template && template.Find(paramName) is IParameterNode param)
			{
				param.Value.Clear();
				param.Value.AddText(GetPatchVersion(job) + '\n');
				patchPage.Text = parser.ToRaw();
				patchPage.Save("Update " + paramName, true);
			}
		}

		public static string TimeToText(int time) => ((double)time).ToString("0,.#", CultureInfo.InvariantCulture);
		#endregion

		#region Private Methods
		private static void GetPlaceCategory(Site site, PlaceCollection places, PlaceInfo placeInfo)
		{
			if (placeInfo.CategoryName == null)
			{
				return;
			}

			var cat = new PageCollection(site);
			cat.GetCategoryMembers(placeInfo.CategoryName);
			foreach (var member in cat)
			{
				if (member.Namespace == UespNamespaces.Online)
				{
					// TODO: Take another look at this later. Error catching added here that triggered on [[Online:Farm House]]. Having a bad day and not sure if this is the right thing to do.
					try
					{
						if (places[member.PageName] is Place place)
						{
							if (place.PlaceType == PlaceType.Unknown)
							{
								place.PlaceType = placeInfo.Type;
							}
							else
							{
								Debug.WriteLine($"Multiple place types on page: {member.FullPageName}");
							}
						}
					}
					catch (InvalidOperationException)
					{
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