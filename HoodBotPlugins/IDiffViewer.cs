namespace RobinHood70.HoodBotPlugins
{
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
		/// <param name="diff">The diff content whose text(s) should be compared.</param>
		void Compare(DiffContent diff);

		/// <summary>Waits for the user to be finished with the curent comparison.</summary>
		/// <remarks>If applicable, this method should wait for the current diff window to be closed before returning.</remarks>
		void Wait();
		#endregion
	}
}
