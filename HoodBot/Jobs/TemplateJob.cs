namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	[method: JobInfo("Template Job")]
	public abstract class TemplateJob(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Protected Abstract Properties
		protected abstract string TemplateName { get; }
		#endregion

		#region Protected Override Methods
		protected override void LoadPages()
		{
			var title = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Template], this.TemplateName);
			this.Pages.GetBacklinks(title.FullPageName(), BacklinksTypes.EmbeddedIn);
		}

		protected override void ParseText(SiteParser parser)
		{
			foreach (var template in parser.FindSiteTemplates(this.TemplateName))
			{
				this.ParseTemplate(template, parser);
			}
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void ParseTemplate(SiteTemplateNode template, SiteParser parser);
		#endregion
	}
}