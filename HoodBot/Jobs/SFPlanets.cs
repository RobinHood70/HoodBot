namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class SFPlanets : CreateOrUpdateJob<SFPlanets.Planet>
	{
		#region Constructors
		[JobInfo("SF Planets", "Starfield")]
		public SFPlanets(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "planet";

		protected override string EditSummary => "Create/update planet";
		#endregion

		#region Protected Override Methods
		protected override bool IsValid(ContextualParser parser, Planet data) => parser.FindSiteTemplate("Planet Infobox") is not null;

		protected override IDictionary<Title, Planet> LoadItems()
		{
			var biomes = GetBiomes();

			return GetPlanets_NewCsv(biomes);
			// Uncomment to retain access to import old csv
			//return GetPlanets(biomes);
		}

		/// <summary>
		/// Retrieves dictionary of biomes from biomedata.csv
		/// </summary>		
		private static Dictionary<int, ICollection<string>> GetBiomes()
		{
			var csv = new CsvFile() { Encoding = Encoding.GetEncoding(1252) };
			var biomes = new Dictionary<int, ICollection<string>>();
			csv.Load(LocalConfig.BotDataSubPath("Starfield/biomedata.csv"), true, 1);
			foreach (var row in csv)
			{
				var id = CombineId(row["Star ID"], row["Planet ID"]);
				if (!biomes.TryGetValue(id, out var biome))
				{
					biome = new SortedSet<string>(StringComparer.Ordinal);
					biomes.Add(id, biome);
				}

				biome.Add(row["Biome Name"]);
			}
			csv.Clear();

			return biomes;
		}

		/// <summary>
		/// Original code for getting planets from Planets.csv
		/// </summary>
		private Dictionary<Title, Planet> GetPlanets(Dictionary<int, ICollection<string>> biomes)
		{
			var csv = new CsvFile() { Encoding = Encoding.GetEncoding(1252) };
			var items = new Dictionary<Title, Planet>();
			csv.Load(LocalConfig.BotDataSubPath("Starfield/Planets.csv"), true, 0);
			foreach (var row in csv)
			{
				var id = CombineId(row["StarId"], row["PlanetId"]);
				var biome = biomes.TryGetValue(id, out var val) ? val : Array.Empty<string>();
				var itemType = row["Type"]
					.Replace("G.", "Giant", StringComparison.Ordinal)
					.Replace("Aster.", "Asteroid", StringComparison.Ordinal);
				var gravity = double.TryParse(row["Gravity"], CultureInfo.CurrentCulture, out var grav)
					? grav
					: (double?)null;
				var magnetosphere = row["MagField"];
				if (string.IsNullOrWhiteSpace(magnetosphere))
				{
					magnetosphere = "Unknown";
				}

				var planet = new Planet(row["Name"], row["StarName"], row["Orbits"], itemType, gravity, string.Empty, string.Empty, magnetosphere, string.Empty, string.Empty, string.Empty, Array.Empty<string>(), biome, int.Parse(row["Primary"], CultureInfo.CurrentCulture));
				var name = "Starfield:" + planet.Name;
				items.Add(TitleFactory.FromUnvalidated(this.Site, name), planet);
			}

			return items;
		}

		/// <summary>
		/// Planets_infobox.csv
		/// https://discord.com/channels/972159937310502923/1123008833963429940/1160714775215484951
		/// </summary>
		/// <remarks>PlanetId,StarId,Name,System,Orbits,Type,Gravity,Temperature_Class,Temperature_Degrees,Atmosphere_Pressure,Atmosphere_Type,Magnetosphere,Fauna,Flora,Water,Traits</remarks>
		internal Dictionary<Title, Planet> GetPlanets_NewCsv(Dictionary<int, ICollection<string>> biomes)
		{
			var csv = new CsvFile();
			var items = new Dictionary<Title, Planet>();
			csv.Load(LocalConfig.BotDataSubPath("Starfield/Planets_Infobox.csv"), true, 0);
			foreach (var row in csv)
			{
				var id = CombineId(row["StarId"], row["PlanetId"]);
				var biome = biomes.TryGetValue(id, out var val) ? val : Array.Empty<string>();
				var gravity = double.TryParse(row["Gravity"], CultureInfo.CurrentCulture, out var grav)
					? grav
					: (double?)null;
				var traits = row["Traits"].Split(":", StringSplitOptions.RemoveEmptyEntries).ToArray();
				var pressure = row["Atmosphere_Pressure"].Replace("Std", "Standard").Replace("Extr", "Extreme");

				var planet = new Planet(
					row["Name"],
					row["System"],
					row["Orbits"],
					row["Type"],
					gravity,
					$"{row["Temperature_Class"]} ({row["Temperature_Degrees"]}°)",
					$"{pressure} {row["Atmosphere_Type"]}",
					row["Magnetosphere"],
					row["Fauna"],
					row["Flora"],
					row["Water"],
					traits,
					biome,
					string.IsNullOrEmpty(row["Orbits"]) ? 0 : 1);

				var name = "Starfield:" + planet.Name;
				items.Add(TitleFactory.FromUnvalidated(this.Site, name), planet);
			}

			return items;
		}

		private static int CombineId(string star, string planet) => (int.Parse(star, CultureInfo.CurrentCulture) << 8) + int.Parse(planet, CultureInfo.CurrentCulture);

		protected override string NewPageText(Title title, Planet item) => "{{Planet Infobox\n" +
			"|image=\n" +
			$"|system=\n" +
			$"|type=\n" +
			$"|gravity=\n" +
			"|temp=\n" +
			"|atmosphere=\n" +
			$"|magnetosphere=\n" +
			"|fauna=\n" +
			"|flora=\n" +
			"|water=\n" +
			"|resource=\n" +
			"|trait=\n" +
			"|biome=\n" +
			"|orbits=\n" +
			"|orbital_position=\n" +
			"}}\n\n{{Stub|Planet}}";

		protected override void PageLoaded(ContextualParser parser, Planet item)
		{
			var template = parser.FindSiteTemplate("Planet Infobox") ?? throw new InvalidOperationException();
			var biomes = item.Biomes.Count == 0
				? string.Empty
				: ("\n* " + string.Join("\n* ", item.Biomes));
			var gravityText = item.Gravity == 0
				? string.Empty
				: item.Gravity?.ToStringInvariant() ?? string.Empty;
			template.UpdateIfEmpty("system", item.StarName);
			template.UpdateIfEmpty("type", item.Type);
			template.UpdateIfEmpty("gravity", gravityText);
			template.Update("magnetosphere", item.Magnetosphere); // Because original CSV was wrong we want to full replace
			template.Update("temp", item.Temperature); // Because we have degrees we want to full replace
			template.Update("atmosphere", item.Atmosphere);
			template.UpdateIfEmpty("flora", item.Flora);
			template.UpdateIfEmpty("fauna", item.Fauna);
			template.UpdateIfEmpty("water", item.Water);

			// I expect many planets to have incomplete traits, or to reference gravitational anomaly, which is not set in stone, so full replace
			template.Update("trait", string.Join(Environment.NewLine, item.Traits.Select(t => "{{Trait|" + t + "}}")));
			if (item.Primary > 0)
			{
				template.Update("orbits", item.Orbits);
				template.Update("orbital_position", string.Empty);
			}
			else
			{
				template.Remove("orbits");
			}

			template.AddIfNotExists("biomes", biomes + "\n", ParameterFormat.NoChange);
		}
		#endregion

		#region Internal Records
		internal sealed record Planet(string Name, string StarName, string Orbits, string Type, double? Gravity, string Temperature,
			string Atmosphere, string Magnetosphere, string Fauna, string Flora, string Water, ICollection<string> Traits, ICollection<string> Biomes, int Primary);
		#endregion
	}
}