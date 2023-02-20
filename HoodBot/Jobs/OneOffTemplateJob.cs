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

		#region Public Override Properties
		public override string? LogDetails => "Update " + this.TemplateName;

		public override string LogName => "One-Off Template Job";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Update remaining Future Links to Lore Links";

		protected override string TemplateName => "Future Link";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			template.Title.Clear();
			template.Title.AddText("Lore Link");
		}
		#endregion
	}
}