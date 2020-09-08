namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Reflection;
	using System.Text.RegularExpressions;
	using Microsoft.Extensions.Configuration;
	using MySql.Data.MySqlClient;
	using RobinHood70.HoodBot.Jobs.Design;
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
		private static readonly Regex ColourCode = new Regex(@"\A\|c[0-9A-F]{6}(.*?)\|r\Z", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
		private static readonly Regex TrailingDigits = new Regex(@"\s*\d+\Z", RegexOptions.None, DefaultRegexTimeout);
		#endregion

		#region Fields
		private static readonly Regex BonusFinder = new Regex(@"\s*Current [Bb]onus:.*?\.", RegexOptions.None, DefaultRegexTimeout);
		private static readonly Regex SpaceFixer = new Regex(@"[\n\ ]+", RegexOptions.None, DefaultRegexTimeout);
		private static string? esoLogConnectionString; // = ConfigurationManager.ConnectionStrings["EsoLog"].ConnectionString;
		private static string? patchVersion;
		#endregion

		#region Public Properties
		public static string EsoLogConnectionString
		{
			get
			{
				if (esoLogConnectionString == null)
				{
					var folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
					var builder = new ConfigurationBuilder()
						.SetBasePath(folder)
						.AddJsonFile("connectionStrings.json", false, false);
					var config = builder.Build();
					esoLogConnectionString = config.GetConnectionString("EsoLog");
				}

				return esoLogConnectionString;
			}
		}

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

		public static IEnumerable<PlaceInfo> PlaceInfo { get; } = new PlaceInfo[]
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
						npc.UnknownLocations.Add(location, count);
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
			var nameClash = new HashSet<string>(StringComparer.Ordinal);
			foreach (var row in RunQuery("SELECT id, name, gender, difficulty, ppDifficulty, ppClass, reaction FROM uesp_esolog.npc WHERE level != -1"))
			{
				var name = (string)row["name"];
				if (!ColourCode.IsMatch(name) && !TrailingDigits.IsMatch(name))
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
			var pageLoadOptions = new PageLoadOptions(PageModules.Default | PageModules.Custom);
			var pageCreator = (job.Site.PageCreator as MetaTemplateCreator) ?? new MetaTemplateCreator();
			var patchPage = (VariablesPage)patchTitle.Load(pageLoadOptions, pageCreator)[0];
			patchVersion = patchPage.MainSet?["number"] is string version
				? version
				: throw new InvalidOperationException("Could not find patch version on page.");
			return patchPage;
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
			var pageLoadOptions = new PageLoadOptions(PageModules.Custom, true);
			var pageCreator = new MetaTemplateCreator("alliance", "settlement", "titlename", "type", "zone");
			var places = new PageCollection(site, pageLoadOptions, pageCreator);
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

		public static string HarmonizeDescription(string desc) => SpaceFixer.Replace(BonusFinder.Replace(desc, string.Empty), " ");

		public static void ParseNpcLocations(NpcCollection npcData, PlaceCollection places)
		{
			foreach (var npc in npcData)
			{
				var locCopy = new Dictionary<string, int>(npc.UnknownLocations, StringComparer.Ordinal);
				foreach (var kvp in locCopy)
				{
					var key = kvp.Key;
					if (places[key] is Place place)
					{
						npc.Places.Add(place, kvp.Value);
						npc.UnknownLocations.Remove(key);
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

			var patchPage = GetPatchPage(job);
			var parser = new ContextualParser(patchPage);
			var paramName = "bot" + pageType;
			if (parser.FindTemplate("Online Patch") is TemplateNode template && template.FindParameter(paramName) is ParameterNode param)
			{
				param.Value.Clear();
				param.Value.AddText(GetPatchVersion(job) + '\n');
				patchPage.Text = parser.GetText();
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