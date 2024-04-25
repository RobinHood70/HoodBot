namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	[method: JobInfo("One-Off Parse Job")]
	public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Public Override Properties
		public override string LogDetails => "Add Planet Navbox";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages()
		{
			this.Shuffle = true;
			this.Pages.GetBacklinks("Template:Planet Infobox", BacklinksTypes.EmbeddedIn, true);
		}

		protected override void ParseText(ContextualParser parser)
		{
			if (parser.FindSiteTemplate("Planet Navbox") is not null)
			{
				return;
			}

			var loc = parser.FindLastIndex<SiteTemplateNode>(t => t.TitleValue.PageNameEquals("Stub"));
			if (loc == -1)
			{
				parser.AddText("\n\n{{Planet Navbox}}");
				return;
			}

			parser.InsertText(loc, "{{Planet Navbox}}\n");
		}
		#endregion
	}
}