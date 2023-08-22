namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
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
		}
		#endregion

		#region Protected Override Methods
		protected override string EditSummary => "Create star";

		protected override void LoadPages()
		{
			var fileName = LocalConfig.BotDataSubPath("stars.csv");
			var starsFile = new CsvFile();
			starsFile.ReadFile(fileName, true);
			foreach (var star in starsFile)
			{
				this.stars.Add(star["proper"], star);
			}
		}

		protected override void PageMissing(Page page)
		{
			var star = this.stars[page.Title.PageName];
			page.Text = $"{{{{System Infobox\n" +
			$"|eid={star["id"]}\n" +
			$"|name={star["proper"]}\n" +
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