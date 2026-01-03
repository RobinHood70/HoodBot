namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

internal sealed class SFPlanets_Old : CreateOrUpdateJob<SFPlanets_Old.Planet>
{
	#region Static Fields
	private static readonly Regex BiomeFinder = new(
		@".*?\: BIOM - .*? ""(?<type>.*?)"" Chance: (?<chance>\d+)%.*?( <(?<fauna>\d+) fauna>)?( <(?<flora>\d+) flora>)?\Z",
		RegexOptions.ExplicitCapture,
		Globals.DefaultRegexTimeout);
	#endregion

	#region Constructors
	[JobInfo("Planets", "Starfield Old")]
	public SFPlanets_Old(JobManager jobManager)
		: base(jobManager)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
	}
	#endregion

	#region Internal Methods

	// From Planets_infobox.csv per https://discord.com/channels/972159937310502923/1123008833963429940/1160714775215484951.
	// PlanetId, StarId, Name, System, Orbits, Type, Gravity, Temperature_Class, Temperature_Degrees, Atmosphere_Pressure, Atmosphere_Type, Magnetosphere, Fauna, Flora, Water, Traits
	internal Dictionary<Title, Planet> ReadEchelar(Dictionary<string, ICollection<string>> biomes)
	{
		var items = new Dictionary<Title, Planet>();
		var csvName = GameInfo.Starfield.ModFolder + "Planets_Infobox.csv";
		if (!File.Exists(csvName))
		{
			return items;
		}

		var csv = new CsvFile(csvName)
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		csv.Load();
		foreach (var row in csv)
		{
			var id = row["Name"];
			var biome = biomes.TryGetValue(id, out var val) ? val : [];
			_ = double.TryParse(row["Gravity"], CultureInfo.CurrentCulture, out var grav);
			var traits = row["Traits"].Split(":", StringSplitOptions.RemoveEmptyEntries).ToArray();
			var pressure = row["Atmosphere_Pressure"]
				.Replace("Std", "Standard", StringComparison.Ordinal)
				.Replace("Extr", "Extreme", StringComparison.Ordinal);

			var planet = new Planet(
				row["Name"],
				row["System"],
				row["Orbits"],
				row["Type"],
				grav,
				$"{row["Temperature_Class"]} ({row["Temperature_Degrees"]}°)",
				$"{pressure} {row["Atmosphere_Type"]}",
				row["Magnetosphere"],
				row["Water"],
				traits,
				biome,
				string.IsNullOrEmpty(row["Orbits"]) ? 0 : 1)
			{
				Fauna = row["Fauna"],
				Flora = row["Flora"]
			};

			var name = "Starfield:" + planet.Name;
			items.Add(TitleFactory.FromUnvalidated(this.Site, name), planet);
		}

		return items;
	}
	#endregion

	#region Protected Override Methods
	protected override string? GetDisambiguator(Planet item) => "planet";

	protected override string GetEditSummary(Page page) => "Create/update planet";

	protected override TitleDictionary<Planet> GetExistingItems() => [];

	protected override void GetExternalData()
	{
		// NOTE: This was a hasty conversion to the new format that just stuffs everything in GetExternalData(). If used again in the future, it should probably be separated into its proper GetExternal/GetExisting/GetNew components.
		var biomes = GetBiomes();
		this.Items.AddRange(this.ReadEchelar(biomes));
		this.ReadWip3(this.Items);
	}

	protected override TitleDictionary<Planet> GetNewItems() => [];

	protected override void ItemPageLoaded(SiteParser parser, Planet item)
	{
		var template = parser.FindTemplate("Planet Infobox") ?? throw new InvalidOperationException();
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
		template.UpdateIfEmpty("flora", item.Flora ?? string.Empty);
		template.UpdateIfEmpty("fauna", item.Fauna ?? string.Empty);
		template.UpdateIfEmpty("water", item.Water);

		// I expect many planets to have incomplete traits, or to reference gravitational anomaly, which is not set in stone, so full replace
		template.Update("trait", string.Join('\n', item.Traits.Select(t => "{{Trait|" + t + "}}")));
		if (item.Primary > 0)
		{
			template.Update("orbits", item.Orbits);
			template.Update("orbital_position", string.Empty);
		}
		else
		{
			template.Remove("orbits");
		}

		if (biomes.Length > 0)
		{
			template.Update("biomes", biomes, ParameterFormat.OnePerLine, false);
		}
	}
	#endregion

	#region Private Static Methods
	protected override string GetNewPageText(Title title, Planet item) => "{{Planet Infobox\n" +
		"|image=\n" +
		"|system=\n" +
		"|type=\n" +
		"|gravity=\n" +
		"|temp=\n" +
		"|atmosphere=\n" +
		"|magnetosphere=\n" +
		"|fauna=\n" +
		"|flora=\n" +
		"|water=\n" +
		"|resource=\n" +
		"|trait=\n" +
		"|biome=\n" +
		"|orbits=\n" +
		"|orbital_position=\n" +
		"}}\n\n{{Stub|Planet}}";
	#endregion

	#region Private Methods

	/// <summary>
	/// Retrieves dictionary of biomes from biomedata.csv.
	/// </summary>
	private static Dictionary<string, ICollection<string>> GetBiomes()
	{
		var biomes = new Dictionary<string, ICollection<string>>(StringComparer.Ordinal);
		var csvName = GameInfo.Starfield.ModFolder + "biomesplanets.csv";
		if (!File.Exists(csvName))
		{
			return biomes;
		}

		var csv = new CsvFile(csvName)
		{
			Encoding = Encoding.GetEncoding(1252),
			HasHeader = false
		};

		csv.Load();
		string? planet = null;
		var biomeList = new List<string>();
		foreach (var row in csv)
		{
			var word = row[0].Trim()[..4];
			if (word.OrdinalEquals("FULL"))
			{
				if (planet is not null)
				{
					biomes.Add(planet, biomeList);
				}

				var line = row[0].Split(TextArrays.Colon, 2);
				planet = line[1].Trim();
				biomeList = [];
			}
			else
			{
				var biomeData = BiomeFinder.Match(row[0]);
				var groups = biomeData.Groups;
				var value = $"{groups["chance"]}% {groups["type"]}";
				var floraFauna = new List<string>();
				if (groups["fauna"].Success)
				{
					floraFauna.Add(groups["fauna"].Value + " fauna");
				}

				if (groups["flora"].Success)
				{
					floraFauna.Add(groups["flora"].Value + " flora");
				}

				if (floraFauna.Count > 0)
				{
					value += " (" + string.Join(", ", floraFauna) + ")";
				}

				biomeList.Add(value);
			}
		}

		if (planet is not null)
		{
			biomes.Add(planet, biomeList);
		}

		return biomes;
	}

	private void ReadWip3(IDictionary<Title, Planet> items)
	{
		var faunaCounts = new Dictionary<string, int>(StringComparer.Ordinal);
		var csv = new CsvFile(GameInfo.Starfield.ModFolder + "sfcreatures_-_wip3.csv")
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		csv.Load();
		foreach (var row in csv)
		{
			var planetName = row["Planet"];
			faunaCounts[planetName] = faunaCounts.TryGetValue(planetName, out var value)
				? ++value
				: 1;
		}

		foreach (var fauna in faunaCounts)
		{
			var title = TitleFactory.FromUnvalidated(this.Site, $"Starfield:{fauna.Key}");
			var planet = items[title];
			planet.Fauna += $" ({fauna.Value})";
		}
	}
	#endregion

	#region Internal Records
	internal sealed record Planet(string Name, string StarName, string Orbits, string Type, double? Gravity, string Temperature,
		string Atmosphere, string Magnetosphere, string Water, ICollection<string> Traits, ICollection<string> Biomes, int Primary)
	{
		public string? Fauna { get; set; }

		public string? Flora { get; set; }
	}
	#endregion
}