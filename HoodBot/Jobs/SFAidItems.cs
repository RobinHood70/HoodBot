namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

internal sealed class SFAidItems : CreateOrUpdateJob<SFItem>
{
	#region Static Fields
	private static readonly Dictionary<string, string> ReplacementNames = new(StringComparer.Ordinal)
	{
		["NOCLUTTER_ResearchUI_Aid_Affl_Multi_RepairingImmobilizer"] = "Repairing Immobilizer (no clutter)",
		["NOCLUTTER_ResearchUI_Chem_Craft_Amp"] = "Amp (no clutter)",
		["NOCLUTTER_ResearchUI_Aid_Affl_Multi_AntibioticPaste"] = "Antibiotic Paste (no clutter)",
		["NOCLUTTER_ResearchUI_Aid_Craft_Boudicca"] = "Boudicca (no clutter)",
		["NOCLUTTER_ResearchUI_Chem_Craft_DwarfHeart"] = "Dwarf Star Heart (no clutter)",
		["NOCLUTTER_ResearchUI_Chem_Craft_Frostwolf"] = "Frostwolf (no clutter)",
		["NOCLUTTER_ResearchUI_Chem_Craft_GiantHeart"] = "Giant Heart (no clutter)",
		["NOCLUTTER_ResearchUI_Chem_Craft_Neurajack"] = "Neurajack (no clutter)",
		["NOCLUTTER_ResearchUI_Aid_Craft_Panacea"] = "Panacea (no clutter)",
		["NOCLUTTER_ResearchUI_Chem_Craft_Panopticon"] = "Panopticon (no clutter)",
		["NOCLUTTER_ResearchUI_Chem_Craft_SuperMassiveHeart"] = "Supermassive Black Heart (no clutter)",
		["PEO_Sus_Drink_Tier_1_Potion"] = "Hydrated (Tier 1)",
		["PEO_Sus_Drink_Tier_2_Potion"] = "Hydrated (Tier 2)",
		["PEO_Sus_Drink_Tier_3_Potion"] = "Hydrated (Tier 3)",
		["PEO_Sus_Food_Tier_1_Potion"] = "Fed (Tier 1)",
		["PEO_Sus_Food_Tier_2_Potion"] = "Fed (Tier 2)",
		["PEO_Sus_Food_Tier_3_Potion"] = "Fed (Tier 3)",
	};
	#endregion

	#region Constructors
	[JobInfo("Aid Items", "Starfield")]
	public SFAidItems(JobManager jobManager)
		: base(jobManager)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		this.NewPageText = GetNewPageText;
		this.OnUpdate = this.UpdateAidItem;
		this.Shuffle = true;
	}
	#endregion

	#region Protected Override Properties
	protected override string? GetDisambiguator(SFItem item) => "aid item";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update Aid Items";

	protected override TitleDictionary<SFItem> GetExistingItems() => [];

	protected override void GetExternalData()
	{
		// NOTE: This was a hasty conversion to the new format that just stuffs everything in GetExternalData(). If used again in the future, it should probably be separated into its proper GetExternal/GetExisting/GetNew components.
		foreach (var item in ReadItems())
		{
			var name = ReplacementNames.TryGetValue(item.EditorId, out var replacement)
				? replacement
				: item.Name;
			if (name.Length != 0)
			{
				var title = TitleFactory.FromUnvalidated(this.Site[StarfieldNamespaces.Starfield], name);
				if (!this.Items.TryAdd(title, item))
				{
					var existing = this.Items[title];
					Debug.WriteLine($"Dupe - {title.FullPageName()}: {existing.EditorId} / {item.EditorId}");
				}
			}
		}
	}

	protected override TitleDictionary<SFItem> GetNewItems() => [];

	protected override bool IsValidPage(SiteParser parser, SFItem item) =>
		parser.FindTemplate("Item Summary") is ITemplateNode template &&
		string.Equals(template.Find("editorid")?.GetValue() ?? throw new InvalidOperationException(), item.EditorId, StringComparison.Ordinal);
	#endregion

	#region Private Static Methods
	private static string GetNewPageText(Title title, SFItem item) => $$$"""
		{{Trail|Items|Aid Items}}{{Item Summary
		|objectid={{{item.FormId}}}
		|editorid={{{item.EditorId}}}
		|type=Aid Item
		|image=<!--SF-item-{{{item.Name}}}.png-->
		|weight={{{item.Weight}}}
		|value={{{item.Value}}}
		|effect=
		|description={{{item.Description}}}
		}}
		[[{{{title.FullPageName()}}}|{{{item.Name}}}]] is an [[Starfield:Aid Items|aid]] [[Starfield:Items|item]].
		""";

	private static IEnumerable<SFItem> ReadItems()
	{
		var csvName = GameInfo.Starfield.ModFolder + "Alchemy.csv";
		if (!File.Exists(csvName))
		{
			yield break;
		}

		var csvFile = new CsvFile(csvName)
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		csvFile.HeaderFieldMap["Unkown1"] = "Value";
		foreach (var row in csvFile.ReadRows())
		{
			yield return new SFItem(row, "Aid Item");
		}
	}

	private void UpdateAidItem(SiteParser parser, SFItem item)
	{
		if (parser.Page.Exists)
		{
		}

		var template = parser.FindTemplate("Item Summary") ?? throw new InvalidOperationException();
		var pagenameParam = template.Find("pagename");
		var pagename = pagenameParam?.GetValue();
		if (parser.Title.PageNameEquals(pagename))
		{
			pagename = null;
			template.Remove("pagename");
		}

		var titleParam = template.Find("title");
		var title = titleParam?.GetValue();
		if ((pagename is null && parser.Title.PageNameEquals(title)) || string.Equals(pagename, title, StringComparison.Ordinal))
		{
			title = null;
			template.Remove("title");
		}

		template.Update("objectid", item.FormId, ParameterFormat.OnePerLine, false);
		template.Update("editorid", item.EditorId, ParameterFormat.OnePerLine, false); // For trimming
		template.Update("weight", item.Weight.ToStringInvariant(), ParameterFormat.OnePerLine, false);
		template.Update("value", item.Value.ToStringInvariant(), ParameterFormat.OnePerLine, false);
		template.UpdateIfEmpty("image", $"<!--SF-item-{item.Name}.png-->", ParameterFormat.OnePerLine);
		var defaultImgdesc = title ?? pagename ?? parser.Title.PageName;
		if (template.Find("imgdesc") is IParameterNode imgdesc &&
			string.Equals(imgdesc.GetValue(), defaultImgdesc, StringComparison.OrdinalIgnoreCase))
		{
			template.Remove("imgdesc");
		}

		if (template.Find("effect") is IParameterNode effectParam)
		{
			var effectText = effectParam.GetValue();
			if (item.Description.Length > 0)
			{
				effectText = effectText.Replace(item.Description, string.Empty, StringComparison.OrdinalIgnoreCase);
			}

			effectText = effectText.Replace("''''", string.Empty, StringComparison.Ordinal).Trim();
			effectParam.SetValue(effectText, ParameterFormat.OnePerLine);
		}

		template.Update("description", item.Description, ParameterFormat.OnePerLine, false);
		template.Sort("objectid", "editorid", "type", "weight", "value", "image", "imgdesc", "effect", "description");
	}
	#endregion
}