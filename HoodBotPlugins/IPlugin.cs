namespace RobinHood70.HoodBotPlugins
{
	/// <summary>A general interface to build plugins which HoodBot can incorporate.</summary>
	public interface IPlugin
	{
		/// <summary>Validates the plugin.</summary>
		/// <returns><c>true</c> if the plugin can be used in the current configuration; otherwise, <c>false</c>.</returns>
		bool ValidatePlugin();
	}
}