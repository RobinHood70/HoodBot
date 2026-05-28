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
		this.AddMove("Online:Crafting Motif 55: Dreadhorn", "Online:Crafting Motif 55: Dreadhorn Style");
		this.AddMove("Online:Crafting Motif 56: Apostle", "Online:Crafting Motif 56: Apostle Style");
		this.AddMove("Online:Crafting Motif 58: Fang Lair", "Online:Crafting Motif 58: Fang Lair Style");
		this.AddMove("Online:Crafting Motif 59: Scalecaller", "Online:Crafting Motif 59: Scalecaller Style");
		this.AddMove("Online:Crafting Motif 60: Worm Cult", "Online:Crafting Motif 60: Worm Cult Style");
		this.AddMove("Online:Crafting Motif 61: Psijic", "Online:Crafting Motif 61: Psijic Style");
		this.AddMove("Online:Crafting Motif 65: Huntsman", "Online:Crafting Motif 65: Huntsman Style");
		this.AddMove("Online:Crafting Motif 66: Silver Dawn", "Online:Crafting Motif 66: Silver Dawn Style");
		this.AddMove("Online:Crafting Motif 67: Welkynar", "Online:Crafting Motif 67: Welkynar Style");
		this.AddMove("Online:Crafting Motif 70: Elder Argonian", "Online:Crafting Motif 70: Elder Argonian Style");
	}
	#endregion
}