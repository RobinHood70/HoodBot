namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoBlankNpcImages : TemplateUsage
	{
		#region Constructors
		[JobInfo("NPCs Missing Image", "ESO")]
		public EsoBlankNpcImages(JobManager jobManager)
			: base(
				  jobManager,
				  ["Online NPC Summary"],
				  true,
				  LocalConfig.BotDataSubPath("ESO NPCs No Images.txt"),
				  false)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override bool ShouldAddPage(ContextualParser parser) =>
			parser.Page.Title.Namespace == UespNamespaces.Online &&
			parser.FindSiteTemplate("Template:Mod Header") is null;

		protected override bool ShouldAddTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			static bool IsWhitespace(IWikiNode node) =>
				node is ITextNode text && text.Text.Trim().Length == 0;

			if (template.Find("image") is not IParameterNode image)
			{
				return false;
			}

			var value = image.Value;
			foreach (var node in value)
			{
				if (node is not ICommentNode && !IsWhitespace(node))
				{
					return false;
				}
			}

			return true;
		}
		#endregion
	}
}