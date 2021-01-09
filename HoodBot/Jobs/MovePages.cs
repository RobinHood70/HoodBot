namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Uesp;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public MovePages(JobManager jobManager)
			: base(jobManager)
		{
			this.DeleteStatusFile();
			this.MoveAction = MoveAction.None;
			this.FollowUpActions = FollowUpActions.Default & ~FollowUpActions.EmitReport;
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements() => this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("MoveList.txt"));
		#endregion
	}
}