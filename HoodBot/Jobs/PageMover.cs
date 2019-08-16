namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class PageMover : PageMoverJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public PageMover(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.MoveOptions = MoveOptions.MoveSubPages | MoveOptions.MoveTalkPage;
		}
		#endregion

		#region Protected Override Methods
		protected override ICollection<Replacement> GetReplacements()
		{
			var retval = new List<Replacement>
			{
				new Replacement(this.Site, "Online:Skeletal Mage (skill)", "Online:Skeletal Mage"),
				new Replacement(this.Site, "Online:Spirit Mender (skill)", "Online:Spirit Mender")
			};

			return retval;
		}
		#endregion
	}
}