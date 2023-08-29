namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;

	internal sealed class SFStars : EditJob
	{
		#region Fields
		private readonly Dictionary<string, CsvRow> stars = new(System.StringComparer.Ordinal);
		#endregion

		#region Constructors
		[JobInfo("SF Stars")]
		public SFStars(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Methods
		protected override string EditSummary => "Create star";

		protected override void LoadPages()
		{
			var fileName = LocalConfig.BotDataSubPath("Starfield/stars.csv");
			var starsFile = new CsvFile();
			starsFile.Load(fileName, true, Encoding.GetEncoding(1252));
			foreach (var star in starsFile)
			{
				var name = star["Name"];
				this.stars.Add(name, star);
			}
		}

		protected override void PageMissing(Page page)
		{
			var star = this.stars[page.Title.PageName];
			page.Text = $"{{{{System Infobox\n" +
			$"|eid={star["FormID"]}\n" +
			$"|name={star["Name"]}\n" +
			$"|class={star["spect"]}\n" +
			$"|id={star["gl"]}\n" +
			$"|temp={star["Temp"]}\n" +
			$"|magnitude={star["absmag"]}\n" +
			"|image=\n" +
			"|level=\n" +
			"|mass=\n" +
			"|moon=\n" +
			"|planet=\n" +
			"|radius=\n" +
			$"}}}}";
		}

		protected override void PageLoaded(Page page)
		{
		}
		#endregion
	}
}