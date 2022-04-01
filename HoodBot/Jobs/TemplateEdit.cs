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

		protected override string TemplateName => "Online Furnishing Summary";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			if (template.Find("skills") is IParameterNode param)
			{
				foreach (var node in param.Value.TextNodes)
				{
					node.Text = node.Text
						.Replace("Woodworking (skill", "Woodworking (skill)", StringComparison.Ordinal)
						.Replace("Woodworking (skill))", "Woodworking (skill)", StringComparison.Ordinal);
				}
			}
		}
		#endregion
	}
}