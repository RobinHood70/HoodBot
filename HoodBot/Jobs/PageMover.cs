namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class PageMover : PageMoverJob
	{
		[JobInfo("Page Mover")]
		public PageMover(Site site, AsyncInfo asyncInfo, TaskActions taskActions)
			: base(site, asyncInfo, taskActions)
		{
		}

		protected override IList<Replacement> GetReplacements()
		{
			var retval = new List<Replacement>();

			return retval;
		}
	}
}
