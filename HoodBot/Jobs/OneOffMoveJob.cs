namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.Robby;

public class OneOffMoveJob : MovePagesJob
{
	#region Constructors
	[JobInfo("One-Off Move Job")]
	public OneOffMoveJob(JobManager jobManager, bool updateUserSpace)
		: base(jobManager, updateUserSpace)
	{
		this.MoveAction = MoveAction.MoveOverExisting;
		this.FollowUpActions = FollowUpActions.Default; // FollowUpActions.CheckLinksRemaining | FollowUpActions.EmitReport | FollowUpActions.FixLinks;
		this.SuppressRedirects = false;
	}
	#endregion

	#region Protected Override Properties
	public override string LogDetails => "Fix incorrect Battleground'''s''' merchants";
	#endregion

	#region Protected Override Methods
	protected override void PopulateMoves()
	{
		this.AddMove($"Online:Battlegrounds Merchants", $"Online:Battleground Merchants");
		this.AddMove($"Online:Battlegrounds Supplies Merchants", $"Online:Battleground Supplies Merchants");
	}

	protected override void UpdateLinkText(ITitle page, SiteLink from, SiteLink toLink, bool addCaption)
	{
		base.UpdateLinkText(page, from, toLink, addCaption);
		toLink.Text = toLink.Text?
			.Replace("Battlegrounds Supplies", "Battleground Supplies", StringComparison.Ordinal)
			.Replace("Battlegrounds supplies", "Battleground supplies", StringComparison.Ordinal)
			.Replace("battlegrounds supplies", "battleground supplies", StringComparison.Ordinal)
			.Replace("Battlegrounds Merch", "Battleground Merch", StringComparison.Ordinal)
			.Replace("Battlegrounds merch", "Battleground merch", StringComparison.Ordinal)
			.Replace("battlegrounds merch", "battleground merch", StringComparison.Ordinal);
	}
	#endregion
}