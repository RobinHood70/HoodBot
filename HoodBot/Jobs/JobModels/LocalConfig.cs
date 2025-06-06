namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.IO;

internal static class LocalConfig
{
	#region Public Properties
	public static string BotDataFolder => Environment.ExpandEnvironmentVariables("%BotData%");

	public static string EsoUIArtFolder => Path.Combine(BotDataFolder, "esoui/art");

	public static string WikiIconsFolder => Path.Combine(EsoUIArtFolder, "icons");
	#endregion

	#region Public Methods
	public static string BotDataSubPath(string file) => Path.Combine(BotDataFolder, file);
	#endregion
}