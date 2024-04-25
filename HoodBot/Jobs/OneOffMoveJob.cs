namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;

	[method: JobInfo("One-Off Move Job")]
	public class OneOffMoveJob(JobManager jobManager, bool updateUserSpace) : MovePagesJob(jobManager, updateUserSpace)
	{
		#region Protected Override Methods
		protected override void BeforeMain()
		{
			this.MoveAction = MoveAction.None;
			//// this.SuppressRedirects = false;
			this.FollowUpActions = FollowUpActions.FixLinks | FollowUpActions.UpdateCategoryMembers;
			this.Site.WaitForJobQueue();
			base.BeforeMain();
		}

		protected override string GetEditSummary(Page page) => "Fix incorrect category";

		protected override void PopulateMoves()
		{
			this.AddLinkUpdate("Category:Redirects to Alternate Names", "Category:Redirects from Alternate Names");
		}

		//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
		//// this.AddMove(title, newName);
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(LocalConfig.BotDataSubPath("Replacements5.txt"));
		#endregion
	}
}