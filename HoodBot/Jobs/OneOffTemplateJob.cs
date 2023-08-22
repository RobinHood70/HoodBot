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
		protected override string EditSummary => "Update remaining Future Links to Lore Links";

		protected override string TemplateName => "System Infobox";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			template.AddIfNotExists("image", string.Empty, ParameterFormat.OnePerLine);
			template.AddIfNotExists("level", string.Empty, ParameterFormat.OnePerLine);
			template.AddIfNotExists("mass", string.Empty, ParameterFormat.OnePerLine);
			template.AddIfNotExists("moon", string.Empty, ParameterFormat.OnePerLine);
			template.AddIfNotExists("planet", string.Empty, ParameterFormat.OnePerLine);
			template.AddIfNotExists("radius", string.Empty, ParameterFormat.OnePerLine);
		}
		#endregion
	}
}