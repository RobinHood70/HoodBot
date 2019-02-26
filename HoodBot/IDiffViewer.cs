namespace RobinHood70.HoodBot
{
	public interface IDiffViewer
	{
		#region Properties
		string Name { get; }
		#endregion

		#region Methods
		void Compare(string oldText, string newText, string oldTitle, string newTitle);

		void Initialize();

		bool Validate();

		void Wait();
		#endregion
	}
}
