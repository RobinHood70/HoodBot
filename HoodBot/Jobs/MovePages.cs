namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using RobinHood70.Robby;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public MovePages(JobManager jobManager)
			: base(jobManager)
		{
			this.DeleteStatusFile();
			this.MoveAction = MoveAction.None;
			this.MoveDelay = 0;
			this.FollowUpActions = FollowUpActions.FixLinks | FollowUpActions.EmitReport | FollowUpActions.CheckLinksRemaining | FollowUpActions.UpdateCaption | FollowUpActions.UpdatePageNameCaption;
		}
		#endregion

		#region Protected Override Methods
		protected override void UpdateLinkText(Page page, Title oldTitle, SiteLink newLink, bool addCaption)
		{
			if (newLink.Text != null)
			{
				base.UpdateLinkText(page, oldTitle, newLink, addCaption);
				Debug.WriteLine($"{page.PageName} / {oldTitle.PageName} / {newLink.Text}");
			}
		}

		protected override void PopulateReplacements()
		{
			this.AddReplacement("Online:Chaotic Whirlwind (Perfected)", "Online:Perfected Chaotic Whirlwind");
			this.AddReplacement("Online:Concentrated Force (Perfected)", "Online:Perfected Concentrated Force");
			this.AddReplacement("Online:Defensive Position (Perfected)", "Online:Perfected Defensive Position");
			this.AddReplacement("Online:Disciplined Slash (Perfected)", "Online:Perfected Disciplined Slash");
			this.AddReplacement("Online:Piercing Spray (Perfected)", "Online:Perfected Piercing Spray");
			this.AddReplacement("Online:Timeless Blessing (Perfected)", "Online:Perfected Timeless Blessing");
		}
		#endregion
	}
}