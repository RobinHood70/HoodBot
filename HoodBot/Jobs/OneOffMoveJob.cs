namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.Robby;

public class OneOffMoveJob : MovePagesJob
{
	#region Constructors
	[JobInfo("One-Off Move Job")]
	public OneOffMoveJob(JobManager jobManager, bool updateUserSpace)
		: base(jobManager, updateUserSpace)
	{
		this.MoveAction = MoveAction.None;
		this.FollowUpActions = FollowUpActions.CheckLinksRemaining | FollowUpActions.EmitReport | FollowUpActions.FixLinks | FollowUpActions.RetainDirectLinkText;
	}
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update Lore Links (bot-assisted)";

	protected override void PopulateMoves() => this.AddMove("Lore:Herne", "Lore:Herne (island)");
	#endregion
}