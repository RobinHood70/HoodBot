namespace RobinHood70.HoodBotPlugins
{
	using RobinHood70.Robby;

	/// <summary>An interface for diff viewer plugins to be used with HoodBot.</summary>
	public interface IDiffViewer : IPlugin
	{
		#region Properties
		/// <summary>Gets the human-readable name of this diff viewer.</summary>
		/// <value>The name.</value>
		/// <remarks>If appropriate, this name should be internationalized.</remarks>
		string Name { get; }
		#endregion

		#region Methods
		/// <summary>Compares the specified texts.</summary>
		/// <param name="page">The page whose text and most-recent revision should be compared.</param>
		/// <param name="editSummary">The edit summary for the edit (for browser-based diff viewers where a save may be desirable).</param>
		/// <param name="isMinor">Whether the edit should be marked as minor (for browser-based diff viewers where a save may be desirable).</param>
		/// <param name="editToken">An edit token (for browser-based diff viewers where a save may be desirable). May be <see langword="null"/>for non-browser diff viewers.</param>
		void Compare(Page page, string editSummary, bool isMinor, string editToken);

		/// <summary>Waits for the user to be finished with the curent comparison.</summary>
		/// <remarks>If applicable, this method should wait for the current diff window to be closed before returning.</remarks>
		void Wait();
		#endregion
	}
}
