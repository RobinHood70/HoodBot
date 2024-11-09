namespace RobinHood70.HoodBot.Jobs.JobModels
{
	public sealed class GameInfo(string baseFolder, string modFolder, string modTemplateName, string modTemplate)
	{
		#region Constructors
		public GameInfo(string baseFolder, string modFolder, string modTemplateName)
			: this(
				  baseFolder,
				  modFolder,
				  modTemplateName,
				  modTemplateName.Length == 0 ? string.Empty : "{{" + modTemplateName + "}}")
		{
		}
		#endregion

		#region Static Properties
		public static GameInfo Eso => new(
			LocalConfig.BotDataFolder,
			LocalConfig.BotDataFolder,
			string.Empty);

		public static GameInfo Starfield => new(
			LocalConfig.BotDataSubPath(@"Starfield\"),
			LocalConfig.BotDataSubPath(@"Starfield\ShatteredSpaceData2\ShatteredSpace\"),
			"SS");
		#endregion

		#region Public Properties
		public string BaseFolder { get; } = baseFolder;

		public string ModFolder { get; } = modFolder;

		public string ModHeader => "{{Mod Header|" + this.ModTemplateName + "}}";

		public string ModTemplate { get; } = modTemplate;

		public string ModTemplateName { get; } = modTemplateName;
		#endregion
	}
}
