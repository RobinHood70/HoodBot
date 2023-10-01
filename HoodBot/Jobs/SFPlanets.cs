namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
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
		[JobInfo("Planets", "Starfield")]
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

				var planet = new Planet(row["Name"], row["StarName"], itemType, gravity, magnetosphere, biome, row["Orbits"], int.Parse(row["Primary"], CultureInfo.CurrentCulture));
				var name = "Starfield:" + planet.Name;
				items.Add(TitleFactory.FromUnvalidated(this.Site, name), planet);
			}

			return items;

			static int CombineId(string star, string planet) => (int.Parse(star, CultureInfo.CurrentCulture) << 8) + int.Parse(planet, CultureInfo.CurrentCulture);
		}

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
			template.UpdateIfEmpty("magnetosphere", item.Magnetosphere);
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
		internal sealed record Planet(string Name, string StarName, string Type, double? Gravity, string Magnetosphere, ICollection<string> Biomes, string Orbits, int Primary);
		#endregion
	}
}