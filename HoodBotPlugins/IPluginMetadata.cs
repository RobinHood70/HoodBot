namespace RobinHood70.HoodBotPlugins
{
	/// <summary>A metadata interface, as required by MEF.</summary>
	public interface IPluginMetadata
	{
		/// <summary>Gets the display name for the plugin. </summary>
		/// <value>The display name.</value>
		string DisplayName { get; }
	}
}
