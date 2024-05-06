namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	[method: JobInfo("One-Off Move Job")]
	public class OneOffMoveJob(JobManager jobManager, bool updateUserSpace) : MovePagesJob(jobManager, updateUserSpace)
	{
		#region Protected Override Methods
		protected override void BeforeMain()
		{
			this.MoveAction = MoveAction.None;
			base.BeforeMain();
		}

		protected override string GetEditSummary(Page page) => "Link past redirect";

		protected override void OnFromPageOrphaned(Page page)
		{
			if (page.Exists)
			{
				page.Title.Delete("Unnecessary redirect");
			}
		}

		protected override void PopulateMoves()
		{
			var redirects = new PageCollection(this.Site, PageModules.Links);
			redirects.GetNamespace(MediaWikiNamespaces.File, Filter.Only, "ON-icon-lead-");
			foreach (var page in redirects)
			{
				this.AddLinkUpdate(page.Title, page.Links[0]);
			}
		}

		//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
		//// this.AddMove(title, newName);
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(LocalConfig.BotDataSubPath("Replacements5.txt"));
		#endregion
	}
}