namespace RobinHood70.HoodBot.Jobs
{
	public class QuickMove : MovePagesJob
	{
		#region Constructors
		[JobInfo("Quick Move")]
		public QuickMove(JobManager jobManager, string from, string to)
				: base(jobManager)
		{
			this.FollowUpActions = FollowUpActions.FixLinks;
			this.EditSummaryMove = $"Move to [[{to}]]";
			this.AddMove(from, to);
		}
		#endregion

		#region Protected Override Methods

		protected override void PopulateMoves()
		{
		}
		#endregion
	}
}