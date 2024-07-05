namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	[method: JobInfo("One-Off Parse Job")]
	public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Public Override Properties
		public override string LogDetails => "Add navbox";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages()
		{
			this.Pages.GetBacklinks("Template:NPC Summary", WikiCommon.BacklinksTypes.EmbeddedIn);
		}

		protected override void ParseText(ContextualParser parser)
		{
			if (parser.FindSiteTemplate("Npc Navbox") is null)
			{
				var index = parser.FindIndex<SiteTemplateNode>(t => t.TitleValue.PageNameEquals("Stub"));
				if (index == -1)
				{
					index = parser.Count;
				}

				var text = index == -1 ? "\n\n{{Npc Navbox}}" : "{{Npc Navbox}}\n\n";
				parser.InsertText(index, text);
			}
		}
		#endregion
	}
}