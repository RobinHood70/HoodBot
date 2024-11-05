namespace RobinHood70.HoodBot.Jobs.JobModels
{
	internal static class Eso
	{
		public static string BaseFolder { get; } = LocalConfig.BotDataFolder;

		public static string ModFolder { get; } = BaseFolder;

		public static string ModTemplate => ModTemplateName.Length == 0
			? string.Empty
			: "{{" + ModTemplateName + "}}";

		public static string ModTemplateName => string.Empty;
	}
}
