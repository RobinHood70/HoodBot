namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.Robby.Design;

public class QuickMove : MovePagesJob
{
	#region Constructors
	[JobInfo("Quick Move")]
	public QuickMove(JobManager jobManager, string from, string to, bool renameOnly)
			: base(jobManager, false)
	{
		this.Pages.SetLimitations(LimitationType.None);
		this.MoveAction = renameOnly ? MoveAction.None : MoveAction.MoveSafely;
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