namespace RobinHood70.HoodBot.Jobs.Tasks
{
	using RobinHood70.Robby;

	public interface IWikiTask
	{
		Site Site { get; }

		void Execute();
	}
}
