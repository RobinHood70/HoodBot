namespace RobinHood70.HoodBot.Parser
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class ContextualParser : NodeCollection
	{
		#region Constructors
		public ContextualParser(Site site)
			: base(null)
		{
			ThrowNull(site, nameof(site));
			this.Site = site;
		}

		public ContextualParser(ISimpleTitle title, NodeCollection nodes)
			: base(null, nodes)
		{
			ThrowNull(title, nameof(title));
			this.Title = title;
			this.Site = title.Namespace.Site;
		}
		#endregion

		#region Public Properties
		public Dictionary<string, Func<string>> MagicWordResolvers { get; } = new Dictionary<string, Func<string>>();

		public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>();

		public Dictionary<string, Func<string>> TemplateResolvers { get; } = new Dictionary<string, Func<string>>();

		public Site Site { get; }

		public ISimpleTitle? Title { get; set; }
		#endregion

		#region Public Static Methods
		public static ContextualParser FromPage(Page page)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(page.Text, nameof(page), nameof(page.Text));
			return FromText(page, page.Text, null);
		}

		public static ContextualParser FromText(ISimpleTitle title, string text, bool? inclusion)
		{
			ThrowNull(title, nameof(title));
			ThrowNull(text, nameof(text));
			var nodes = WikiTextParser.Parse(text, inclusion, false);
			return new ContextualParser(title, nodes);
		}
		#endregion

		#region Public Methods
		public bool AddCategory(string category)
		{
			ThrowNull(category, nameof(category));
			var catTitle = new FullTitle(this.Site, MediaWikiNamespaces.Category, category, false);
			LinkedListNode<IWikiNode>? lastCategory = null;
			foreach (var link in this.FindAllLinked<LinkNode>())
			{
				var linkNode = (LinkNode)link.Value;
				var titleText = WikiTextVisitor.Value(linkNode.Title);
				var title = new FullTitle(this.Site, titleText);
				if (title.Namespace == MediaWikiNamespaces.Category)
				{
					if (title.PageName == catTitle.PageName)
					{
						return false;
					}

					lastCategory = link;
				}
			}

			var newCat = LinkNode.FromParts(catTitle.ToString());
			if (lastCategory == null)
			{
				this.AddLast(new TextNode("\n\n"));
				this.AddLast(newCat);
			}
			else
			{
				this.AddAfter(lastCategory, newCat);
			}

			return true;
		}
		#endregion
	}
}