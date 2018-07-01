namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;

	// Shell for testing purposes.
	public abstract class WikiJob : IWikiJob
	{
		protected WikiJob(Site site, params IWikiTask[] tasks)
		{
			this.Site = site;
			this.Tasks = new List<IWikiTask>(tasks);
		}

		public Site Site { get; }

		public IReadOnlyList<IWikiTask> Tasks { get; }

		public abstract void AddToTitles(TitleCollection titles);
	}
}