namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	[method: JobInfo("One-Off Parse Job")]
	public class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Public Override Properties
		public override string LogDetails => "Add missing Mod Headers";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages()
		{
			var from = new DateTime(2024, 6, 4, 2, 46, 0, DateTimeKind.Utc);
			var to = DateTime.UtcNow;
			var titles = new TitleCollection(this.Site);
			var contributions = this.Site.User?.GetContributions(from, to) ?? throw new InvalidOperationException();
			foreach (var contribution in contributions)
			{
				if (contribution.Title.Namespace == UespNamespaces.Online)
				{
					titles.Add(contribution.Title);
				}
			}

			this.Pages.GetTitles(titles);
		}

		protected override void ParseText(ContextualParser parser)
		{
			if (parser.FindSiteTemplate("Mod Header") is null)
			{
				var insertLoc = parser.FindIndex<SiteTemplateNode>(node => node.TitleValue.PageNameEquals("Online NPC Summary") || node.TitleValue.PageNameEquals("Online Furnishing Summary"));
				if (insertLoc != -1)
				{
					var newNode = parser.Factory.TemplateNodeFromWikiText("{{Mod Header|Gold Road}}");
					parser.Insert(insertLoc, newNode);
				}
			}
		}
		#endregion
	}
}