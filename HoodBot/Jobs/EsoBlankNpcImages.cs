namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("NPCs Missing Image", "ESO")]
	internal sealed class EsoBlankNpcImages(JobManager jobManager) : TemplateUsage(
		jobManager,
		["Online NPC Summary"],
		true,
		LocalConfig.BotDataSubPath("ESO NPCs No Images.txt"),
		false)
	{
		#region Protected Override Methods
		protected override bool ShouldAddPage(SiteParser parser) =>
			parser.Page.Title.Namespace == UespNamespaces.Online &&
			parser.FindSiteTemplate("Template:Mod Header") is null;

		protected override bool ShouldAddTemplate(SiteTemplateNode template, SiteParser parser)
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