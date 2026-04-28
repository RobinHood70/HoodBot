namespace RobinHood70.HoodBot.Jobs;

public class OneOffMoveJob : MovePagesJob
{
	#region Constructors
	[JobInfo("One-Off Move Job")]
	public OneOffMoveJob(JobManager jobManager, bool updateUserSpace)
		: base(jobManager, updateUserSpace)
	{
		this.MoveAction = MoveAction.None;
		this.FollowUpActions = FollowUpActions.Default; // FollowUpActions.CheckLinksRemaining | FollowUpActions.EmitReport | FollowUpActions.FixLinks;
	}
	#endregion

	#region Protected Override Methods
	protected override void PopulateMoves() => this.AddLinkUpdate($"Lore:Shade", $"Lore:Void-Shade");
	#endregion
}