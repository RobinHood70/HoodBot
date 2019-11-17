namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class PageMover : PageMoverJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public PageMover(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.MoveOptions = MoveOptions.MoveSubPages | MoveOptions.MoveTalkPage;
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			this.AddReplacement("Online:Skeletal Mage (skill)", "Online:Skeletal Mage");
			this.AddReplacement("Online:Spirit Mender (skill)", "Online:Spirit Mender");
		}
		#endregion
	}
}