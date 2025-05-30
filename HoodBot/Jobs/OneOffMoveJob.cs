namespace RobinHood70.HoodBot.Jobs;
using RobinHood70.Robby;

[method: JobInfo("One-Off Move Job")]
public class OneOffMoveJob(JobManager jobManager, bool updateUserSpace) : MovePagesJob(jobManager, updateUserSpace)
{
	#region Protected Override Methods
	protected override bool BeforeMain()
	{
		this.FollowUpActions = FollowUpActions.EmitReport | FollowUpActions.FixLinks;
		this.MoveAction = MoveAction.MoveSafely;
		return base.BeforeMain();
	}

	protected override string GetEditSummary(Page page) => "Update links";

	protected override void PopulateMoves()
	{
		var cat = new TitleCollection(this.Site);
		cat.GetCategoryMembers("Online-Icons-Skill Styles", false);
		foreach (var item in cat)
		{
			if (item.PageName.Contains("-skill-"))
			{
				this.AddMove(item, item.FullPageName().Replace("-skill-", "-skill style-"));
			}
		}
	}
	#endregion
}