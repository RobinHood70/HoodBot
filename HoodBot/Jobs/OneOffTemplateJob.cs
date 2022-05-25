namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby.Parser;

	public class OneOffTemplateJob : TemplateJob
	{
		#region Constructors
		[JobInfo("One-Off Template Job")]
		public OneOffTemplateJob(JobManager jobManager)
				: base(jobManager)
		{
		}
		#endregion

		#region Public Properties
		public override string? LogDetails => "Update " + this.TemplateName;

		public override string LogName => "One-Off Template Job";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Update for changes in template";

		protected override string TemplateName => "Template";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
		}
		#endregion
	}
}