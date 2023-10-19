namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	internal sealed class SFRedirects : CreateOrUpdateJob<string>
	{
		#region Constructors
		[JobInfo("Redirects", "Starfield")]
		public SFRedirects(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => null;
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create redirect";

		protected override bool IsValid(ContextualParser parser, string item) => true;

		protected override IDictionary<Title, string> LoadItems()
		{
			var items = new SortedDictionary<Title, string>();
			var csv = new CsvFile();
			csv.Load(LocalConfig.BotDataSubPath("Starfield/Effect_redirects.csv"), true);
			foreach (var row in csv)
			{
				var pageName = row["Page Name"]
					.Replace("<Alias=", string.Empty, System.StringComparison.Ordinal)
					.Replace(">", string.Empty, System.StringComparison.Ordinal);
				items.Add(TitleFactory.FromUnvalidated(this.Site, pageName).Title, row["Page Content"]);
			}

			return items;
		}

		protected override string NewPageText(Title title, string item) => item;
		#endregion
	}
}