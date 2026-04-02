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

	#region Protected Override Properties
	public override string LogDetails => "Fix incorrect Battleground'''s''' merchants";
	#endregion

	#region Protected Override Methods
	protected override void PopulateMoves()
	{
		this.AddLinkUpdate($"Online:Battlegrounds Merchants", $"Online:Battleground Merchants");
		this.AddLinkUpdate($"Online:Battlegrounds Supplies Merchants", $"Online:Battleground Supplies Merchants");
	}
	#endregion
}