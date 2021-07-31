namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class LabelNameRemover : TemplateJob
	{
		#region Constructors
		[JobInfo("Label Name Fixer")]
		public LabelNameRemover(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Remove redundant imgdesc";

		protected override string TemplateName => "Online Collectible Summary";
		#endregion

		#region Protected Override Methods
		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parsedPage)
		{
			if (template.Find("titlename") != null)
			{
				Debug.WriteLine(parsedPage.Context.FullPageName());
			}
			else if (template.Find("imgdesc") is IParameterNode imgdesc &&
				string.Equals(imgdesc.Value.ToValue().Trim(), parsedPage.Context.LabelName(), StringComparison.Ordinal))
			{
				template.Remove("imgdesc");
			}
		}
		#endregion
	}
}
