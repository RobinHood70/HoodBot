namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public static class Extensions
	{
		public static TitleParts BacklinkTitleToParts(this IBacklinkNode node, Site site)
		{
			ThrowNull(node, nameof(node));
			ThrowNull(site, nameof(site));
			var text = WikiTextVisitor.Value(node.Title);
			return node is TemplateNode ? new TitleParts(site, MediaWikiNamespaces.Template, text) : new TitleParts(site, text);
		}
	}
}
