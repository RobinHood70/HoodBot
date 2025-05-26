namespace RobinHood70.HoodBot.Jobs;
using RobinHood70.Robby;

[method: JobInfo("One-Off Move Job")]
public class OneOffMoveJob(JobManager jobManager, bool updateUserSpace) : MovePagesJob(jobManager, updateUserSpace)
{
	#region Protected Override Methods
	protected override bool BeforeMain()
	{
		this.FollowUpActions = FollowUpActions.CheckLinksRemaining | FollowUpActions.EmitReport | FollowUpActions.FixLinks | FollowUpActions.UpdateSameNamedText | FollowUpActions.NeedsCategoryMembers;
		this.MoveAction = MoveAction.None;
		return base.BeforeMain();
	}

	protected override string GetEditSummary(Page page) => "Update categories";

	protected override void PopulateMoves()
	{
		this.AddLinkUpdate("Category:Legacy of the Dragonborn", "Category:Skyrim Mod-Legacy of the Dragonborn-Images");
	}
	#endregion
}