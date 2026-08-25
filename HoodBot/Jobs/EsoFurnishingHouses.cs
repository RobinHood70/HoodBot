namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon;

[method: JobInfo("Furnishing Houses", "ESO")]
internal sealed class EsoFurnishingHouses(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Static Fields
	private static readonly string[] CsvTitles = ["Category", "Item", "House"];
	#endregion

	#region Protected Override Methods
	protected override void Main()
	{
		var categories = this.LoadCategories();
		var catToTitle = CreateUrlEncodeMap(categories);
		var housingPages = this.LoadHousingPages();

		var lines = new List<string>();
		foreach (var page in housingPages)
		{
			var variables = (VariablesPageModule)page.Custom[VariablesPageModule.PropertyName];
			foreach (var variable in variables.MainSet)
			{
				if (!variable.Key.StartsWith("has_", StringComparison.Ordinal))
				{
					continue;
				}

				var varKey = variable.Key[4..];
				if (varKey.OrdinalEquals("furnished"))
				{
					continue;
				}

				if (!catToTitle.TryGetValue(varKey, out var itemTitle))
				{
					Debug.WriteLine($"Title mismatch for {varKey} on page {page.Title}");
					continue;
				}

				var category = categories[itemTitle];
				lines.Add($"{category}\t{itemTitle.PageName}\t{page.Title.PageName}");
			}
		}

		lines.Sort(StringComparer.Ordinal);

		SaveToCsv(lines);
	}
	#endregion

	#region Private Static Methods
	private static Dictionary<string, Title> CreateUrlEncodeMap(TitleDictionary<string> categories)
	{
		var map = new Dictionary<string, Title>(StringComparer.Ordinal);
		foreach (var category in categories)
		{
			var preName = category.Key.LabelName().ToLowerInvariant();
			var encoded = Uri.EscapeDataString(preName)
				.Replace("%20", "_", StringComparison.Ordinal)
				.Replace("%2C", ",", StringComparison.Ordinal)
				.Replace("%3A", ":", StringComparison.Ordinal)
			; // Wiki encoding is slightly different than standard URL encoding

			if (encoded.Length > 46)
			{
				encoded = encoded[..46];
			}

			map[encoded] = category.Key;
		}

		// Manual redirects, since adding them automatically would be a pain
		map["breton_sconce,_grand"] = map["breton_candle,_grand"];
		map["colovian_bookshelf_,_noble_filled"] = map["colovian_bookshelf,_noble_filled"];
		map["imperial_banner,_akatosh"] = map["imperial_banner,_arkay"];
		map["mortar_and_pestle,_paper_making"] = map["pulp_masher,_paper_making"];
		map["sapling,_healthy_forest"] = map["saplings,_healthy_forest"];
		map["solitude_cabinet,_narrow_open_filled"] = map["solitude_bookcase,_narrow_open_filled"];
		map["telvanni_sconce,_fungal_standing"] = map["telvanni_candle,_fungal_standing"];

		return map;
	}

	private static void SaveToCsv(List<string> lines)
	{
		var fileName = Path.Combine(LocalConfig.BotDataSubPath("Furnishing Houses.csv"));
		var csvFile = new CsvFile(fileName) { CsvTitles };
		foreach (var line in lines)
		{
			var fields = line.Split('\t');
			csvFile.Add(fields);
		}

		csvFile.Save();
	}
	#endregion

	#region Private Methods
	private TitleDictionary<string> LoadCategories()
	{
		var categories = new TitleDictionary<string>();
		var housingPages = this.Site.GetMetaVariables(PageModules.None, false, "cat", "subcat");
		housingPages.GetBacklinks("Template:Online Furnishing Houses", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);
		foreach (var page in housingPages)
		{
			var variables = (VariablesPageModule)page.Custom[VariablesPageModule.PropertyName];
			var cat = variables.GetVariable("cat");
			var subcat = variables.GetVariable("subcat");
			if (cat is null)
			{
				if (subcat is not null)
				{
					Debug.WriteLine("WTF on page " + page.Title);
				}

				continue;
			}

			var category = subcat is null ? cat : $"{cat} ({subcat})";
			categories[page.Title] = category;
		}

		return categories;
	}

	private PageCollection LoadHousingPages()
	{
		var housingPages = this.Site.GetMetaVariables(PageModules.None, false);
		housingPages.GetCategoryMembers("Online-Places-Player Houses");

		return housingPages;
	}
	#endregion
}