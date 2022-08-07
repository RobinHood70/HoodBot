namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

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
		protected override string EditSummary => "Fix link templates";

		protected override string TemplateName => "Lore Link";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			template.RenameParameter("ONLink", "ONlink");
			//// template.RenameParameter("LoreLink", "LOlink");
			template.RenameParameter("SILink", "SIlink");
		}
		#endregion
	}
}