namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

internal sealed class SFWeapons : CreateOrUpdateJob<List<CsvRow>>
{
	#region Constructors
	[JobInfo("Weapons", "Starfield")]
	public SFWeapons(JobManager jobManager)
		: base(jobManager)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
	}
	#endregion

	#region Protected Override Properties
	protected override string? GetDisambiguator(List<CsvRow> item) => "weapon";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Create weapon page";

	protected override TitleDictionary<List<CsvRow>> GetExistingItems() => [];

	protected override void GetExternalData()
	{
		// NOTE: This was a hasty conversion to the new format that just stuffs everything in GetExternalData(). If used again in the future, it should probably be separated into its proper GetExternal/GetExisting/GetNew components.
		var csv = new CsvFile(GameInfo.Starfield.ModFolder + "Weapons.csv")
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		foreach (var row in csv.ReadRows())
		{
			var name = row["Name"];
			var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
			if (name.Length > 0)
			{
				var itemList = this.Items.TryGetValue(title, out var list) ? list : [];
				itemList.Add(row);
				this.Items[title] = itemList;
			}
		}
	}

	protected override TitleDictionary<List<CsvRow>> GetNewItems() => [];

	protected override string GetNewPageText(Title title, List<CsvRow> itemList)
	{
		var sb = new StringBuilder();
		foreach (var item in itemList)
		{
			BuildTemplate(sb, item);
		}

		if (sb.Length > 0)
		{
			sb.Remove(0, 14);
		}

		return $$$"""
			{{Trail|Items|Weapons}}{{{sb}}}

			The [[{{{title.FullPageName()}}}|]] is a [[Starfield:Weapons|weapon]].

			{{Starfield Weapons}}
			{{Stub|Weapon}}
			""";
	}

	protected override void ItemPageLoaded(SiteParser parser, List<CsvRow> list)
	{
		// Currently designed for insert only, no updating. Template code has to be duplicated here as well as on NewPageText so that it passes validity checks but also handles insertion correctly.
		var insertPos = parser.LastIndexOf<ITemplateNode>(t => t.GetTitle(parser.Site) == "Template:Item Summary");
		if (insertPos == -1)
		{
			throw new InvalidOperationException("Item Summary template not found.");
		}

		insertPos++;
		foreach (var row in list)
		{
			if (FindMatchingTemplate(parser, row) is null)
			{
				var sb = new StringBuilder();
				BuildTemplate(sb, row);
				var text = sb.ToString();
				var newNodes = parser.Parse(text);
				parser.InsertRange(insertPos, newNodes);
				insertPos += newNodes.Count;
			}
		}
	}
	#endregion

	#region Private Static Methods
	private static void BuildTemplate(StringBuilder sb, CsvRow item) => sb
		.Append("\n\n{{NewLine}}\n")
		.Append("{{Item Summary\n")
		.Append($"|objectid={UespFunctions.FixFormId(item["FormID"])}\n")
		.Append($"|editorid={item["EditorID"].Trim()}\n")
		.Append("|type={{Huh}}\n")
		.Append("|image=\n")
		.Append("|imgdesc=\n")
		.Append($"|weight={item["Weight"]}\n")
		.Append($"|value={item["Value"]}\n")
		.Append("|physicalw={{Huh}}\n")
		.Append($"|ammo={item["Ammo"]}\n")
		.Append($"|capacity={item["MagSize"]}\n")
		.Append("|firerate={{Huh}}\n")
		.Append("|range={{Huh}}\n")
		.Append("|accuracy={{Huh}}\n")
		.Append("|mods={{Huh}}\n")
		.Append("}}");

	private static ITemplateNode? FindMatchingTemplate(SiteParser parser, CsvRow row)
	{
		var templates = parser.FindTemplates("Item Summary");
		foreach (var template in templates)
		{
			var edid = template.GetValue("editorid")?.Trim();
			if (edid.OrdinalICEquals(row["EditorID"]))
			{
				return template;
			}
		}

		return null;
	}
	#endregion
}