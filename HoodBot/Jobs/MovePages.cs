namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Move Pages")]
		public MovePages(JobManager jobManager)
				: base(jobManager)
		{
			this.EditSummaryMove = "Match page name to item";
			this.Site.WaitForJobQueue();
		}
		#endregion

		#region Protected Override Methods
		protected override void PageLoaded(EditJob job, Page page)
		{
			// TODO: Nothing to do here. May be a good candidate for a new job type.
		}

		protected override void PopulateMoves() =>
			this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("Replacements.txt"), ReplacementActions.Move);
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("Comma Replacements5.txt"));
		#endregion
	}
}