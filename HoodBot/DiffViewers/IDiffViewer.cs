namespace RobinHood70.HoodBot.DiffViewers
{
	using RobinHood70.Robby;

	public interface IDiffViewer
	{
		#region Properties
		string Name { get; }

		Page Page { get; }
		#endregion

		#region Methods
		void Compare();

		void Wait();
		#endregion
	}
}
