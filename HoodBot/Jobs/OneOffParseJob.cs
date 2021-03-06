namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
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
		protected override string EditSummary => "Update link";
		#endregion

		#region Protected Override Methods

		protected override void LoadPages()
		{
			this.Pages.GetBacklinks("Oblivion:Undead Dungeons", WikiCommon.BacklinksTypes.Backlinks, true, CommonCode.Filter.Any);
			this.Pages.Remove("UESPWiki:Bot Requests");
		}

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			foreach (var link in parsedPage.LinkNodes)
			{
				var fullLink = SiteLink.FromLinkNode(this.Site, link);
				if (string.Equals(fullLink.Fragment, "Restoration Ayleid Chest", System.StringComparison.Ordinal) ||
					string.Equals(fullLink.Fragment, "Restoration Chest", System.StringComparison.Ordinal))
				{
					link.Title.Clear();
					link.Title.AddRange(fullLink.With(this.Site[UespNamespaces.Oblivion], "Dungeons").ToLinkNode().Title);
				}
			}
		}
		#endregion
	}
}