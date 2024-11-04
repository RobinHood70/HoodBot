namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Label Name Fixer")]
	public class LabelNameRemover(JobManager jobManager) : TemplateJob(jobManager)
	{
		#region Protected Override Properties
		protected override string TemplateName => "Online Collectible Summary";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Remove redundant imgdesc";

		protected override void ParseTemplate(SiteTemplateNode template, SiteParser parser)
		{
			if (template.Find("titlename") != null)
			{
				Debug.WriteLine(parser.Page.Title.FullPageName());
			}
			else if (template.GetValue("imgdesc").OrdinalEquals(parser.Title.LabelName()))
			{
				template.Remove("imgdesc");
			}
		}
		#endregion
	}
}