namespace RobinHood70.HoodBot.Jobs
{
	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public MovePages(JobManager jobManager)
			: base(jobManager)
		{
			this.DeleteStatusFile();
			this.MoveAction = MoveAction.None;
			this.MoveDelay = 100;
			this.FollowUpActions = FollowUpActions.FixLinks | FollowUpActions.EmitReport;
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements() => this.AddReplacement("Online:Reanimated Statue", "Online:Animated Statue");
		#endregion
	}
}