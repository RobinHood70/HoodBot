namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class TemplateEdit : TemplateJob
	{
		[JobInfo("Template Edit")]
		public TemplateEdit(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override string TemplateName => "Icon";

		protected override string EditSummary => "Use built-in replacement";

		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parsedPage)
		{
			if (template.Find(1) is IParameterNode param &&
				param.Value.ToRaw() is var value &&
				string.Equals(value, "furniture", StringComparison.Ordinal))
			{
				param.SetValue("furn");
			}
		}
	}
}
