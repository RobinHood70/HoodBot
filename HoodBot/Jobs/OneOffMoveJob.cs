namespace RobinHood70.HoodBot.Jobs;

public class OneOffMoveJob : MovePagesJob
{
	#region Constructors
	[JobInfo("One-Off Move Job")]
	public OneOffMoveJob(JobManager jobManager, bool updateUserSpace)
		: base(jobManager, updateUserSpace)
	{
		this.MoveAction = MoveAction.MoveSafely;
		this.FollowUpActions = FollowUpActions.Default; // FollowUpActions.CheckLinksRemaining | FollowUpActions.EmitReport | FollowUpActions.FixLinks;
	}
	#endregion

	#region Protected Override Methods
	protected override void PopulateMoves()
	{
		string[] roman = ["I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII"];
		for (var i = 0; i <= 11; i++)
		{
			var suffix = roman[i];
			this.AddMove("Tamriel Data:King Edward, part " + suffix, "Tamriel Data:King Edward, Part " + suffix);
		}
	}
	#endregion
}