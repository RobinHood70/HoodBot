namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	public abstract class ParsedPageJob : EditJob
	{
		#region Constructors
		protected ParsedPageJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void ParseText(ContextualParser parser);
		#endregion

		#region Protected Override Methods
		protected override void PageLoaded(Page page)
		{
			ContextualParser parser = new(page);
			this.ParseText(parser);
			parser.UpdatePage();
		}
		#endregion
	}
}