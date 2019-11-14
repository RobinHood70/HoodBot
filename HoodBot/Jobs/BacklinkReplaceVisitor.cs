namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;

	internal class BacklinkReplaceVisitor : NodeVisitor
	{
		private readonly Site site;
		private readonly Dictionary<Title, Title> textReplacements = new Dictionary<Title, Title>(SimpleTitleEqualityComparer.Instance);

		public BacklinkReplaceVisitor(Site site, Dictionary<Title, Title> replacements)
		{
			this.site = site;
			foreach (var replacement in replacements)
			{
				this.textReplacements.Add(replacement.Key, replacement.Value);
			}
		}

		public override void Visit(LinkNode node)
		{
			this.VisitBacklink(node, false);
			base.Visit(node);
		}

		public override void Visit(TemplateNode node)
		{
			this.VisitBacklink(node, true);
			base.Visit(node);
		}

		private void VisitBacklink(IBacklinkNode parent, bool isTemplate)
		{
			// TODO: Fix this to only add text to the link if on a non-redirect talk page.
			var titleText = WikiTextVisitor.Value(parent.Title);
			var title = new Title(this.site, titleText);
			if (this.textReplacements.TryGetValue(title, out var replacement))
			{
				var newName = (isTemplate && title.Namespace == MediaWikiNamespaces.Template && replacement.Namespace == MediaWikiNamespaces.Template)
					? replacement.PageName
					: replacement.FullPageName;
				var newNode = new TextNode(newName);
				parent.Title.ReplaceAllWithOne<TextNode>(newNode);

				if (!isTemplate)
				{
					if (parent.Parameters.Count == 0)
					{
						var paramNode = new ParameterNode(1, new List<IWikiNode>() { new TextNode(title.PageName) });
						parent.Parameters.AddLast(paramNode);
					}
				}
			}
		}
	}
}
