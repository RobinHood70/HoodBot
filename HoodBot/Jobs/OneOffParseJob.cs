namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	[method: JobInfo("One-Off Parse Job")]
	public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Public Override Properties
		public override string LogDetails => "Remove Trails";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Remove trail already handled by template";

		protected override void LoadPages()
		{
			this.Pages.GetBacklinks("Template:Game Book", BacklinksTypes.EmbeddedIn);
		}

		protected override void ParseText(SiteParser parser)
		{
			parser.RemoveTemplates("Trail");
		}
		#endregion
	}
}