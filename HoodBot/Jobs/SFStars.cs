namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	internal sealed class SFStars : CreateOrUpdateJob<CsvRow>
	{
		#region Constructors
		[JobInfo("Stars", "Starfield")]
		public SFStars(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			this.NewPageText = GetNewPageText;
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "star";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create star";

		protected override IDictionary<Title, CsvRow> LoadItems()
		{
			var items = new Dictionary<Title, CsvRow>();
			var csv = new CsvFile(Starfield.ModFolder + "stars.csv")
			{
				Encoding = Encoding.GetEncoding(1252)
			};

			csv.Load();
			foreach (var row in csv)
			{
				var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + row["Name"]);
				items.Add(title, row);
			}

			return items;
		}

		protected override bool IsValid(ContextualParser parser, CsvRow item) => parser.FindSiteTemplate("System Infobox") is not null;
		#endregion

		#region Private Static Methods
		private static string GetNewPageText(Title title, CsvRow item) =>
			"{{Trail|Places}}\n" +
			"{{System Infobox\n" +
			$"|eid={item["FormID"]}\n" +
			$"|name={item["Name"]}\n" +
			$"|class={item["Spectral"]}\n" +
			$"|id={item["Gliese"]}\n" +
			$"|temp={item["Temperature"]}\n" +
			$"|magnitude={item["AbsMag"]}\n" +
			"|image=\n" +
			"|level=\n" +
			"|mass=\n" +
			"|moon=\n" +
			"|planet=\n" +
			"|radius=\n" +
			"}}\n\n" +
			"{{NewLine}}\n" +
			"{{System Table}}";
		#endregion
	}
}