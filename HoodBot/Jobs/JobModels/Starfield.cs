namespace RobinHood70.HoodBot.Jobs.JobModels
{
	internal static class Starfield
	{
		public static string BaseFolder { get; } = LocalConfig.BotDataSubPath(@"Starfield\");

		public static string ModFolder { get; } = BaseFolder + @"ShatteredSpaceData1\ShatteredSpace\";
	}
}
