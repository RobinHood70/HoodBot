namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class TemplateEdit : TemplateJob
	{
		#region Constructors
		[JobInfo("Template Edit")]
		public TemplateEdit(JobManager jobManager)
				: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Fix bot error";

		protected override string TemplateName => "Furnishing Link";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			if (template.Find(1) is IParameterNode param)
			{
				var value = param.Value.ToRaw();
				value = value
					.Replace("ON-item-furnishing-", string.Empty, StringComparison.Ordinal)
					.Replace("ON-furnishing-", string.Empty, StringComparison.Ordinal)
					.Replace(".jpg", string.Empty, StringComparison.Ordinal);
				param.Value.Clear();
				param.Value.AddText(value);
			}
		}
		#endregion
	}
}