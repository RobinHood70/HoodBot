namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
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
		this.NewPageText = GetNewPageText;
		this.OnUpdate = UpdateWeapon;
	}
	#endregion

	#region Protected Override Properties
	protected override string? Disambiguator => "weapon";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Create weapon page";

	protected override bool IsValid(SiteParser parser, List<CsvRow> item) => parser.FindTemplate("Item Summary") is not null;

	protected override IDictionary<Title, List<CsvRow>> LoadItems()
	{
		var items = new Dictionary<Title, List<CsvRow>>();
		var csv = new CsvFile(GameInfo.Starfield.ModFolder + "Weapons.csv")
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		csv.Load();
		foreach (var row in csv)
		{
			var name = row["Name"];
			var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
			if (name.Length > 0)
			{
				var itemList = items.TryGetValue(title, out var list) ? list : [];
				itemList.Add(row);
				items[title] = itemList;
			}
		}

		return items;
	}
	#endregion

	#region Private Static Methods
	private static void BuildTemplate(StringBuilder sb, CsvRow item)
	{
		sb
			.Append("{{Item Summary\n")
			.Append($"|objectid={item["FormID"][2..]}\n")
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
			.Append("}}\n");
	}

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

	private static string GetNewPageText(Title title, List<CsvRow> itemList)
	{
		var sb = new StringBuilder();
		foreach (var item in itemList)
		{
			BuildTemplate(sb, item);
		}

		return $"{{{{Trail|Items|Weapons}}}}{sb}The [[Starfield:{title.PageName}|]] is a [[Starfield:Weapons|weapon]].\n\n{{{{Stub|Weapon}}}}";
	}

	private static void UpdateWeapon(SiteParser parser, List<CsvRow> list)
	{
		// Currently designed for insert only, no updating. Template code has to be duplicated here as well as on NewPageText so that it passes validity checks but also handles insertion correctly.
		var insertPos = parser.FindIndex<ITemplateNode>(t => t.GetTitle(parser.Site) == "Template:Item Summary");
		foreach (var row in list)
		{
			if (FindMatchingTemplate(parser, row) is null)
			{
				var sb = new StringBuilder();
				BuildTemplate(sb, row);
				var newNodes = parser.Parse(sb.ToString());
				parser.InsertRange(insertPos, newNodes);
				insertPos += newNodes.Count;
			}
		}
	}
	#endregion
}