namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	[method: JobInfo("One-Off Parse Job")]
	public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Public Override Properties
		public override string LogDetails => "Fix duplicate sections";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages()
		{
			this.Pages.GetBacklinks("Template:Online File", WikiCommon.BacklinksTypes.EmbeddedIn);
		}

		protected override void PageLoaded(Page page)
		{
			page.Text = page.Text
				.Replace("== Summary ==\n{{Online File", "{{Online File")
				.Replace("\n\n== Licensing ==\n{{Zenimage}}", string.Empty)
				;
		}

		protected override void ParseText(ContextualParser parser)
		{
		}
		#endregion
	}
}