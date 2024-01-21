namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby.Design;

	public class OneOffMoveJob : MovePagesJob
	{
		#region Constructors
		[JobInfo("One-Off Move Job")]
		public OneOffMoveJob(JobManager jobManager, bool updateUserSpace)
				: base(jobManager, updateUserSpace)
		{
			this.MoveAction = MoveAction.None;
			this.SuppressRedirects = false;
			this.FollowUpActions |= FollowUpActions.UpdateCaption;
			// this.Site.WaitForJobQueue();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateMoves() => this.AddLinkUpdate(
				TitleFactory.FromValidated(this.Site[UespNamespaces.Online], "Endless Archive"),
				TitleFactory.FromValidated(this.Site[UespNamespaces.Online], "Infinite Archive"));

		//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(LocalConfig.BotDataSubPath("Replacements5.txt"));
		#endregion
	}
}