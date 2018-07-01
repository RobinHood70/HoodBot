namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;

	// Shell for testing purposes.
	public interface IWikiJob : IWikiTask
	{
		Site Site { get; }

		IReadOnlyList<IWikiTask> Tasks { get; }
	}
}
