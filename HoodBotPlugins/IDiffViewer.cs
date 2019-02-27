namespace RobinHood70.HoodBotPlugins
{
	public interface IDiffViewer : IPlugin
	{
		#region Properties
		string Name { get; }
		#endregion

		#region Methods
		void Compare(string oldText, string newText, string oldTitle, string newTitle);

		void Wait();
		#endregion
	}
}
