namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public MovePages(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.MoveOverExisting = false;
			this.DeleteFiles();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements() => this.AddReplacement("Online:World Events", "Online:Encounters");
		#endregion
	}
}