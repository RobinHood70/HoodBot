﻿namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	public abstract class TemplateJob : ParsedPageJob
	{
		#region Constructors
		[JobInfo("Template Job")]
		protected TemplateJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Abstract Properties
		protected abstract string TemplateName { get; }
		#endregion

		#region Protected Override Methods
		protected override void LoadPages()
		{
			var title = Title.Coerce(this.Site, MediaWikiNamespaces.Template, this.TemplateName);
			this.Pages.GetBacklinks(title.FullPageName);
		}

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			foreach (var template in parsedPage.FindTemplates(this.TemplateName))
			{
				this.ParseTemplate(template, parsedPage);
			}
		}

		protected abstract void ParseTemplate(SiteTemplateNode template, ContextualParser parsedPage);
		#endregion
	}
}