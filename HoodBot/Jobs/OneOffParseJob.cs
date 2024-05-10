namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("One-Off Parse Job")]
	public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Public Override Properties
		public override string LogDetails => "You should never see this";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages()
		{
			this.Pages.GetCategoryMembers("Online-Empty NPC Pages");
		}

		protected override void ParseText(ContextualParser parser)
		{
			if (parser.FindSiteTemplate("Online NPC Summary") is not SiteTemplateNode template)
			{
				throw new InvalidOperationException();
			}

			var templateText = WikiTextVisitor.Raw(template);
			if (templateText.Contains("Northern Elsweyr", StringComparison.OrdinalIgnoreCase) ||
				templateText.Contains("Scrivener's Hall", StringComparison.OrdinalIgnoreCase) ||
				templateText.Contains("Southern Elsweyr", StringComparison.OrdinalIgnoreCase))
			{
				Debug.WriteLine(parser.Page.Title.FullPageName());
			}
		}
		#endregion
	}
}