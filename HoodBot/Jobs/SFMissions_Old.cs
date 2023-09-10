namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	internal sealed class SFMissions_Old : CreateOrUpdateJob<List<CsvRow>>
	{
		#region Constructors
		[JobInfo("SF Missions (Old)", "Starfield")]
		public SFMissions_Old(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "mission";

		protected override string EditSummary => "Create mission page";
		#endregion

		#region Protected Override Methods
		protected override bool IsValid(ContextualParser parser, List<CsvRow> item) => parser.FindSiteTemplate("Mission Header") is not null;

		protected override IDictionary<Title, List<CsvRow>> LoadItems()
		{
			var items = new Dictionary<Title, List<CsvRow>>();
			var csv = new CsvFile() { Encoding = Encoding.GetEncoding(1252) };
			csv.Load(LocalConfig.BotDataSubPath("Starfield/Quests.csv"), true);
			foreach (var row in csv)
			{
				var name = row["Full"]
					.Replace("<Alias=", string.Empty, StringComparison.Ordinal)
					.Replace(">", string.Empty, StringComparison.Ordinal)
					.Replace('[', '(')
					.Replace(']', ')');
				if (name.StartsWith('(') && name.EndsWith(')'))
				{
					name = name[1..^1];
				}

				var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + name);
				if (name.Length > 0 && !name.Contains('_', StringComparison.Ordinal))
				{
					var itemList = items.TryGetValue(title, out var list) ? list : new List<CsvRow>();
					itemList.Add(row);
					items[title] = itemList;
				}
			}

			return items;
		}

		protected override string NewPageText(Title title, List<CsvRow> item)
		{
			var sb = new StringBuilder();
			foreach (var mission in item)
			{
				var name = mission["Full"];
				sb
					.Append("{{NewLine}}\n")
					.Append("{{Mission Header\n");
				if (!string.Equals(title.LabelName(), name, StringComparison.Ordinal))
				{
					sb.Append($"|title={name}\n");
				}

				sb
					.Append("|type=\n")
					.Append("|Giver=\n")
					.Append("|Icon=\n")
					.Append("|Reward={{Huh}}\n")
					.Append($"|ID={mission["EditorID"]}\n")
					.Append("|Prev=\n")
					.Append("|Next=\n")
					.Append("|Loc=\n")
					.Append("|image=\n")
					.Append("|imgdesc=\n")
					.Append("|description=\n")
					.Append("}}\n");
			}

			return sb.ToString()[12..] + "\n{{Stub|Mission}}";
		}
		#endregion
	}
}