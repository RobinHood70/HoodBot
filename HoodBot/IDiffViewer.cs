namespace RobinHood70.HoodBot
{
	public interface IDiffViewer
	{
		#region Methods
		void Compare(string oldText, string newText, string oldTitle, string newTitle);

		void Wait();
		#endregion
	}
}
