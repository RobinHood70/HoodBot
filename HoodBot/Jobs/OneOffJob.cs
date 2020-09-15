namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class OneOffJob : ParsedPageJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override string EditSummary => "Remove end parameter";

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:ESO Antiquity", WikiCommon.BacklinksTypes.EmbeddedIn);

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			ThrowNull(parsedPage, nameof(parsedPage));
			foreach (var template in parsedPage.FindTemplates("ESO Antiquity"))
			{
				template.Remove("end");
			}
		}
		#endregion
	}
}