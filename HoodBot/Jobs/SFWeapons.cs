namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	internal sealed class SFWeapons : CreateOrUpdateJob<List<CsvRow>>
	{
		#region Constructors
		[JobInfo("SF Weapons")]
		public SFWeapons(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "weapon";

		protected override string EditSummary => "Create weapon page";
		#endregion

		#region Protected Override Methods
		protected override bool IsValid(ContextualParser parser, List<CsvRow> item) => parser.FindSiteTemplate("Item Summary") is not null;

		protected override IDictionary<Title, List<CsvRow>> LoadItems()
		{
			var items = new Dictionary<Title, List<CsvRow>>();
			var csv = new CsvFile();
			csv.Load(LocalConfig.BotDataSubPath("Starfield/Weapons.csv"), true, Encoding.GetEncoding(1252));
			foreach (var row in csv)
			{
				var name = row["Name"];
				var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
				if (name.Length > 0)
				{
					var itemList = items.TryGetValue(title, out var list) ? list : new List<CsvRow>();
					itemList.Add(row);
					items[title] = itemList;
				}
			}

			return items;
		}

		protected override string NewPageText(Title title, List<CsvRow> itemList)
		{
			var sb = new StringBuilder();
			foreach (var item in itemList)
			{
				sb
					.Append("{{NewLine}}\n")
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

			return "{{Trail|Items|Weapons}}" + sb.ToString()[12..] + $"The [[Starfield:{title.PageName}|]] is a [[Starfield:Weapons|weapon]].\n\n{{{{Stub|Weapon}}}}";
		}
		#endregion
	}
}