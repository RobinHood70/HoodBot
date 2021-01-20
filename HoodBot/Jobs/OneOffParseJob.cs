namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffParseJob : ParsedPageJob
	{
		#region Constructors
		[JobInfo("One-Off Parse Job")]
		public OneOffParseJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Add new template";
		#endregion

		#region Protected Override Methods

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Blades Effects", WikiCommon.BacklinksTypes.EmbeddedIn, true, CommonCode.Filter.Exclude);

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			var index = parsedPage.Nodes.FindIndex<SiteTemplateNode>(template => template.TitleValue.PageNameEquals("Blades Effects"));
			if (index >= 0)
			{
				parsedPage.Nodes.InsertRange(index, new IWikiNode[]
				{
					parsedPage.Nodes.Factory.TemplateNodeFromParts("Blades Items with Effect"),
					parsedPage.Nodes.Factory.TextNode("\n")
				});
			}
		}
		#endregion
	}
}