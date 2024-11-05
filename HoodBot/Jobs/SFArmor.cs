namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class SFArmor : CreateOrUpdateJob<List<CsvRow>>
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

		protected override bool IsValid(SiteParser parser, List<CsvRow> item) => parser.FindSiteTemplate("Item Summary") is not null;

		protected override IDictionary<Title, List<CsvRow>> LoadItems()
		{
			var items = new Dictionary<Title, List<CsvRow>>();
			var csv = new CsvFile(Starfield.ModFolder + "Armors.csv")
			{
				Encoding = Encoding.GetEncoding(1252)
			};

			csv.Load();
			foreach (var row in csv)
			{
				var name = row["Name"];
				var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
				if (name.Length > 0 && !name.Contains('_', StringComparison.Ordinal))
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
		private static void BuildTemplate(StringBuilder sb, CsvRow armor)
		{
			var itemType = GetItemType(armor);
			sb
				.Append("{{Item Summary\n")
				.Append($"|objectid={armor["FormID"][2..]}\n")
				.Append($"|editorid={armor["EditorID"].Trim()}\n")
				.Append($"|type={itemType}\n")
				.Append("|image=\n")
				.Append("|imgdesc=\n")
				.Append($"|weight={armor["Weight"]}\n")
				.Append($"|value={armor["Value"]}\n")
				.Append("|physical={{Huh}}\n")
				.Append("|energy={{Huh}}\n")
				.Append("|electromagnetic={{Huh}}\n")
				.Append("|radiation={{Huh}}\n")
				.Append("|thermal={{Huh}}\n")
				.Append("|airborne={{Huh}}\n")
				.Append("|corrosive={{Huh}}\n")
				.Append("}}");
		}

		private static SiteTemplateNode? FindMatchingTemplate(SiteParser parser, CsvRow row)
		{
			var templates = parser.FindSiteTemplates("Item Summary");
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

		private static string? GetItemType(CsvRow armor)
		{
			var nameEdid = armor["Name"] + '/' + armor["EditorID"].Trim();
			var itemType =
				nameEdid.Contains("Clothes", StringComparison.Ordinal) ? "Apparel" :
				nameEdid.Contains("Outfit", StringComparison.Ordinal) ? "Apparel" :
				nameEdid.Contains("Skin", StringComparison.OrdinalIgnoreCase) ? "Skin" :
				nameEdid.Contains("Helmet", StringComparison.Ordinal) ? "Helmet" :
				nameEdid.Contains("Pack", StringComparison.Ordinal) ? "Pack" :
				nameEdid.Contains("Spacesuit", StringComparison.OrdinalIgnoreCase) ? "Spacesuit" :
				null;

			if (itemType is null)
			{
				Debug.WriteLine("Item type not found: " + nameEdid);
			}

			return itemType;
		}

		private static string GetNewPageText(Title title, List<CsvRow> item)
		{
			var sb = new StringBuilder();
			foreach (var armor in item)
			{
				BuildTemplate(sb, armor);
			}

			var firstType = GetItemType(item[0]);
			var link = firstType switch
			{
				"Apparel" => "piece of [[Starfield:Apparel|apparel]]",
				"Helmet" => "[[Starfield:Helmet|helmet]]",
				"Pack" => "[[Starfield:Pack|pack]]",
				"Skin" => "[[Starfield:Skin|skin]]",
				"Spacesuit" => "[[Starfield:Spacesuit|spacesuit]]",
				_ => null,
			};

			return $"{{{{Trail|Items|{firstType}}}}}{sb}The [[Starfield:{title.PageName}|]] is a {link}.\n\n{{{{Stub|{firstType}}}}}";
		}

		private static void UpdateArmor(SiteParser parser, List<CsvRow> list)
		{
			// Currently designed for insert only, no updating. Template code has to be duplicated here as well as on NewPageText so that it passes validity checks but also handles insertion correctly.
			var insertPos = parser.FindIndex<SiteTemplateNode>(t => t.Title.PageNameEquals("Item Summary"));
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
}