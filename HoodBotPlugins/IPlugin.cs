namespace RobinHood70.HoodBotPlugins
{
	/// <summary>A general interface to build plugins which HoodBot can incorporate.</summary>
	public interface IPlugin
	{
		/// <summary>Validates the plugin.</summary>
		/// <returns><see langword="true"/> if the plugin can be used in the current configuration; otherwise, <see langword="false"/>.</returns>
		bool ValidatePlugin();
	}
}