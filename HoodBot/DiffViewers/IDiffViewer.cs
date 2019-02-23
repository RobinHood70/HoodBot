namespace RobinHood70.HoodBot.DiffViewers
{
	using RobinHood70.Robby;

	public interface IDiffViewer
	{
		#region Methods
		void Compare(Page page);

		void Wait();
		#endregion
	}
}
