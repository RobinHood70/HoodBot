namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

internal sealed class SFArmor : CreateOrUpdateJob<List<SFItem>>
{
	#region Constructors
	[JobInfo("Armor", "Starfield")]
	public SFArmor(JobManager jobManager)
		: base(jobManager)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		this.NewPageText = GetNewPageText;
		this.OnUpdate = UpdateArmor;
	}
	#endregion

	#region Protected Override Properties
	protected override string? Disambiguator => "armor";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Create armor page";

	protected override bool IsValidPage(SiteParser parser, List<SFItem> item) => parser.FindTemplate("Item Summary") is not null;

	protected override IDictionary<Title, List<SFItem>> LoadItems()
	{
		var items = new Dictionary<Title, List<SFItem>>();
		var csvName = GameInfo.Starfield.ModFolder + "Armors.csv";
		if (!File.Exists(csvName))
		{
			return items;
		}

		var csv = new CsvFile(csvName)
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		foreach (var row in csv.ReadRows())
		{
			var name = row["Name"];
			var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
			if (name.Length > 0 && !name.Contains('_', StringComparison.Ordinal))
			{
				var itemList = items.TryGetValue(title, out var list) ? list : [];
				var itemType = GetItemType(row);
				var armor = new SFItem(row, itemType);
				itemList.Add(armor);
				items[title] = itemList;
			}
		}

		return items;
	}
	#endregion

	#region Private Static Methods
	private static void BuildTemplate(StringBuilder sb, SFItem armor) => sb
		.Append("{{Item Summary\n")
		.Append($"|objectid={armor.FormId}\n")
		.Append($"|editorid={armor.EditorId}\n")
		.Append($"|type={armor.Type}\n")
		.Append("|image=\n")
		.Append("|imgdesc=\n")
		.Append($"|weight={armor.Weight.ToStringInvariant()}\n")
		.Append($"|value={armor.Value.ToStringInvariant()}\n")
		.Append("|physical={{Huh}}\n")
		.Append("|energy={{Huh}}\n")
		.Append("|electromagnetic={{Huh}}\n")
		.Append("|radiation={{Huh}}\n")
		.Append("|thermal={{Huh}}\n")
		.Append("|airborne={{Huh}}\n")
		.Append("|corrosive={{Huh}}\n")
		.Append("}}");

	private static ITemplateNode? FindMatchingTemplate(SiteParser parser, SFItem item)
	{
		var templates = parser.FindTemplates("Item Summary");
		foreach (var template in templates)
		{
			var edid = template.GetValue("editorid")?.Trim();
			if (edid.OrdinalICEquals(item.EditorId))
			{
				return template;
			}
		}

		return null;
	}

	private static string GetItemType(CsvRow armor)
	{
		var nameEdid = armor["Name"] + '/' + armor["EditorID"].Trim();
		var itemType =
			nameEdid.Contains("Clothes", StringComparison.Ordinal) ? "Apparel" :
			nameEdid.Contains("Outfit", StringComparison.Ordinal) ? "Apparel" :
			nameEdid.Contains("Skin", StringComparison.OrdinalIgnoreCase) ? "Skin" :
			nameEdid.Contains("Helmet", StringComparison.Ordinal) ? "Helmet" :
			nameEdid.Contains("Pack", StringComparison.Ordinal) ? "Pack" :
			nameEdid.Contains("Spacesuit", StringComparison.OrdinalIgnoreCase) ? "Spacesuit" :
			string.Empty;

		if (itemType.Length == 0)
		{
			Debug.WriteLine("Item type not found: " + nameEdid);
		}

		return itemType;
	}

	private static string GetNewPageText(Title title, List<SFItem> item)
	{
		var sb = new StringBuilder();
		foreach (var armor in item)
		{
			BuildTemplate(sb, armor);
		}

		var firstType = item[0].Type;
		var link = firstType switch
		{
			"Apparel" => "piece of [[Starfield:Apparel|apparel]]",
			"Helmet" => "[[Starfield:Helmet|helmet]]",
			"Pack" => "[[Starfield:Pack|pack]]",
			"Skin" => "[[Starfield:Skin|skin]]",
			"Spacesuit" => "[[Starfield:Spacesuit|spacesuit]]",
			_ => null,
		};

		return $$$"""
			{{Trail|Items|{{{firstType}}}}}{{{sb}}}
			The [[{{{title.FullPageName()}}}|]] is a {{{link}}}.

			{{Stub|{{{firstType}}}}}
			""";
	}

	private static void UpdateArmor(SiteParser parser, List<SFItem> list)
	{
		// Currently designed for insert only, no updating. Template code has to be duplicated here as well as on NewPageText so that it passes validity checks but also handles insertion correctly.
		var insertPos = parser.IndexOf<ITemplateNode>(t => t.GetTitle(parser.Site).PageNameEquals("Item Summary"));
		foreach (var item in list)
		{
			if (FindMatchingTemplate(parser, item) is ITemplateNode template)
			{
				template.Update("objectid", item.FormId);
				template.Update("weight", item.Weight.ToStringInvariant());
				template.Update("value", item.Value.ToStringInvariant());
			}
			else
			{
				var sb = new StringBuilder();
				BuildTemplate(sb, item);
				var newNodes = parser.Parse(sb.ToString());
				parser.InsertRange(insertPos, newNodes);
				insertPos += newNodes.Count;
			}
		}
	}
	#endregion
}