namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using RobinHood70.Robby;

	// Shell for testing purposes.
	public abstract class WikiTask : IWikiTask
	{
		public abstract void AddToTitles(TitleCollection titles);
	}
}
