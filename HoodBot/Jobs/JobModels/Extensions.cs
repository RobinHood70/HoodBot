namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

#pragma warning disable CA1724 // Type names should not match namespaces
	public static class Extensions
#pragma warning restore CA1724 // Type names should not match namespaces
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
