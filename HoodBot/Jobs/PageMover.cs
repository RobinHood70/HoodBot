namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class PageMover : PageMoverJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public PageMover(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.MoveOverExisting = true;
			this.MoveAction = MoveAction.None;
			this.FollowUpActions &= FollowUpActions.FixLinks;
			DeleteFiles();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements() => this.AddReplacement("Dragonborn:Dragonborn", "Skyrim:Dragonborn");
		#endregion
	}
}