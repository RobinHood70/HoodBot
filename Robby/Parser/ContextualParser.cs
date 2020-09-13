namespace RobinHood70.Robby.Parser
{
	// TODO: See if the low-level parser (StackElement and derivatives) can be re-written to use a Node factory, then create a factory that aceepts Site and emits Site-specific wrappers around SiteTemplateNode and SiteLinkNode. This would vastly simplify a lot of the checking and inline conversion that's currently happening. In addition, SiteTemplateNode and SiteArgumentNode wrappers could add a settable CurrentValue property for use with the resolvers in this class.
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	/// <summary>This is a higher-level parser that works on a NodeCollection, but adds functionality to resolve magic words and templates within the context of the page.</summary>
	public class ContextualParser
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="page">The page to parse.</param>
		public ContextualParser(Page page)
			: this(page ?? throw ArgumentNull(nameof(page)), page.Text, InclusionType.Raw, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle">title</see> the text will be on.</param>
		/// <param name="text">The text to parse.</param>
		public ContextualParser(ISimpleTitle title, string text)
			: this(title ?? throw ArgumentNull(nameof(title)), text, InclusionType.Raw, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="page">The page to parse.</param>
		/// <param name="inclusionType">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		public ContextualParser(Page page, InclusionType inclusionType, bool strictInclusion)
			: this(page ?? throw ArgumentNull(nameof(page)), page.Text, inclusionType, strictInclusion)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle">title</see> the text will be on.</param>
		/// <param name="text">The text to parse.</param>
		/// <param name="inclusionType">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		public ContextualParser(ISimpleTitle title, string text, InclusionType inclusionType, bool strictInclusion)
		{
			this.Context = title ?? throw ArgumentNull(nameof(title));
			this.Nodes = new WikiNodeFactory().Parse(text, inclusionType, strictInclusion);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the title.</summary>
		/// <value>The title.</value>
		/// <remarks>This provides the context for resolving magic words.</remarks>
		public ISimpleTitle Context { get; set; }

		/// <summary>Gets the <see cref="IHeaderNode"/>s on the page.</summary>
		/// <value>The header nodes.</value>
		public IEnumerable<IHeaderNode> HeaderNodes => this.Nodes.FindAll<IHeaderNode>();

		/// <summary>Gets the <see cref="SiteLinkNode"/>s on the page.</summary>
		/// <value>The header nodes.</value>
		public IEnumerable<SiteLinkNode> LinkNodes => this.Nodes.FindAll<SiteLinkNode>();

		/// <summary>Gets a set of functions to evaluate magic words (e.g., <c>{{PAGENAME}}</c>) and resolve them into meaningful values (NOT IMPLEMENTED).</summary>
		/// <value>The magic word resolvers.</value>
		public IDictionary<string, Func<string>> MagicWordResolvers { get; } = new Dictionary<string, Func<string>>(StringComparer.Ordinal);

		/// <summary>Gets the underlying parser <see cref="NodeCollection"/> holding page contents.</summary>
		/// <value>The NodeCollection.</value>
		public NodeCollection Nodes { get; }

		/// <summary>Gets a set parameter (e.g., <c>{{{1|}}}</c>) names and the values to be used when resolving them (NOT IMPLEMENTED).</summary>
		/// <value>The parameters.</value>
		public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

		/// <summary>Gets the current Site from the Title.</summary>
		/// <value>The site.</value>
		/// <remarks>This property is a direct link to Title and will therefore change if the Title's Site does. Changing Sites within a session may produce unexpected results.</remarks>
		public Site Site => this.Context.Namespace.Site;

		/// <summary>Gets the <see cref="SiteTemplateNode"/>s on the page.</summary>
		/// <value>The header nodes.</value>
		public IEnumerable<SiteTemplateNode> TemplateNodes => this.Nodes.FindAll<SiteTemplateNode>();

		/// <summary>Gets the <see cref="ITextNode"/>s on the page.</summary>
		/// <value>The header nodes.</value>
		public IEnumerable<ITextNode> TextNodes => this.Nodes.FindAll<ITextNode>();

		/// <summary>Gets a set of functions to evaluate templates (e.g., <c>{{PAGENAME}}</c>) and resolve them into meaningful values (NOT IMPLEMENTED).</summary>
		/// <value>The template resolvers.</value>
		public IDictionary<string, Func<string>> TemplateResolvers { get; } = new Dictionary<string, Func<string>>(StringComparer.Ordinal);
		#endregion

		#region Public Methods

		/// <summary>Adds a category to the page.</summary>
		/// <param name="category">The category to add.</param>
		/// <returns><see langword="true"/> if the category was added to the page; <see langword="false"/> if was already on the page.</returns>
		/// <remarks>The category will be added after the last category found on the page, or at the end of the page (preceded by two newlines) if no categories were found.</remarks>
		public bool AddCategory(string category)
		{
			ThrowNull(category, nameof(category));
			var catTitle = Title.Coerce(this.Site, MediaWikiNamespaces.Category, category);
			LinkedListNode<IWikiNode>? lastCategory = null;
			foreach (var link in this.Nodes.FindAllListNodes<SiteLinkNode>(null, false, false, null))
			{
				if (Title.FromBacklinkNode(this.Site, (SiteLinkNode)link.Value) == catTitle)
				{
					return false;
				}

				lastCategory = link;
			}

			var newCat = this.Nodes.Factory.LinkNodeFromParts(catTitle.ToString());
			if (lastCategory == null)
			{
				this.Nodes.AddText("\n\n");
				this.Nodes.AddLast(newCat);
			}
			else
			{
				this.Nodes.AddAfter(lastCategory, newCat);
			}

			return true;
		}

		/// <summary>Finds the first header with the specified text.</summary>
		/// <param name="headerText">Name of the header.</param>
		/// <returns>The first header with the specified text.</returns>
		/// <remarks>This is a temporary function until HeaderNode can be rewritten to work more like other nodes (i.e., without capturing trailing whitespace).</remarks>
		public LinkedListNode<IWikiNode>? FindFirstHeaderLinked(string headerText) => this.Nodes.FindListNode<IHeaderNode>(header => string.Equals(header.GetInnerText(true), headerText, StringComparison.Ordinal), false, true, null);

		/// <summary>Finds the the first link that matches the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The first <see cref="SiteLinkNode"/> that matches the title provided, if found.</returns>
		/// <remarks>The text provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="ISimpleTitle"/>.</remarks>
		public SiteLinkNode? FindLink(string find) => this.FindLink(new TitleParser(this.Site, find));

		/// <summary>Finds the the first link that matches the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The first <see cref="SiteLinkNode"/> that matches the title provided, if found.</returns>
		/// <remarks>As with all <see cref="ISimpleTitle"/> comparisons, only namespace and page name are checked, so trying to find <c>NS:Page</c> will match <c>NS:Page#Fragment</c> and vice versa. To match on the full title in the link, including any interwiki or fragment information, use the overload that takes an <see cref="IFullTitle"/>.</remarks>
		public SiteLinkNode? FindLink(ISimpleTitle find)
		{
			foreach (var link in this.FindLinks(find))
			{
				return link;
			}

			return null;
		}

		/// <summary>Finds the the first link that matches the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The first <see cref="SiteLinkNode"/> that matches the title provided, if found.</returns>
		/// <remarks>The title provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="ISimpleTitle"/>.</remarks>
		public SiteLinkNode? FindLink(IFullTitle find)
		{
			foreach (var link in this.FindLinks(find))
			{
				return link;
			}

			return null;
		}

		/// <summary>Finds all links that match the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The <see cref="SiteLinkNode"/>s that match the title provided, if found.</returns>
		/// <remarks>The text provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="ISimpleTitle"/>.</remarks>
		public IEnumerable<SiteLinkNode> FindLinks(string find) => this.FindLinks(new TitleParser(this.Site, find));

		/// <summary>Finds all links that match the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The <see cref="SiteLinkNode"/>s that match the title provided, if found.</returns>
		/// <remarks>As with all <see cref="ISimpleTitle"/> comparisons, only namespace and page name are checked, so trying to find <c>NS:Page</c> will match <c>NS:Page#Fragment</c> and vice versa. To match on the full title in the link, including any interwiki or fragment information, use the overload that takes an <see cref="IFullTitle"/>.</remarks>
		public IEnumerable<SiteLinkNode> FindLinks(ISimpleTitle find)
		{
			foreach (var link in this.LinkNodes)
			{
				var linkTitle = Title.FromBacklinkNode(this.Site, link);
				if (linkTitle.SimpleEquals(find))
				{
					yield return link;
				}
			}
		}

		/// <summary>Finds all links that match the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The <see cref="SiteLinkNode"/>s that match the title provided, if found.</returns>
		/// <remarks>The title provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="ISimpleTitle"/>.</remarks>
		public IEnumerable<SiteLinkNode> FindLinks(IFullTitle find)
		{
			foreach (var link in this.LinkNodes)
			{
				var linkTitle = FullTitle.FromBacklinkNode(this.Site, link);
				if (linkTitle.FullEquals(find))
				{
					yield return link;
				}
			}
		}

		/// <summary>Finds the the first template that matches the provided title.</summary>
		/// <param name="templateName">The name of the template to find.</param>
		/// <returns>The first <see cref="SiteTemplateNode"/> that matches the title provided, if found.</returns>
		public SiteTemplateNode? FindTemplate(string templateName)
		{
			foreach (var link in this.FindTemplates(templateName))
			{
				return link;
			}

			return null;
		}

		/// <summary>Finds the the first template that matches the provided title and returns its <see cref="LinkedListNode{T}"/>.</summary>
		/// <param name="templateName">The name of the template to find.</param>
		/// <returns>The first <see cref="SiteTemplateNode"/> that matches the title provided, if found.</returns>
		public LinkedListNode<IWikiNode>? FindSiteTemplateNode(string templateName)
		{
			foreach (var link in this.FindSiteTemplateNodes(templateName))
			{
				return link;
			}

			return null;
		}

		/// <summary>Finds all templates that match the provided title.</summary>
		/// <param name="templateName">The name of the template to find.</param>
		/// <returns>The templates that match the title provided, if any.</returns>
		public IEnumerable<SiteTemplateNode> FindTemplates(string templateName)
		{
			var find = new TitleParser(this.Site, MediaWikiNamespaces.Template, templateName);
			foreach (var template in this.TemplateNodes)
			{
				var titleText = template.GetTitleValue();
				var templateTitle = new TitleParser(this.Site, MediaWikiNamespaces.Template, titleText);
				if (templateTitle.SimpleEquals(find))
				{
					yield return template;
				}
			}
		}

		/// <summary>Finds all templates that match the provided title and returns their <see cref="LinkedListNode{T}"/>s.</summary>
		/// <param name="templateName">The name of the template to find.</param>
		/// <returns>The LinkedListNodes that match the title provided, if any.</returns>
		public IEnumerable<LinkedListNode<IWikiNode>> FindSiteTemplateNodes(string templateName)
		{
			var find = new TitleParser(this.Site, MediaWikiNamespaces.Template, templateName);
			foreach (var templateNode in this.Nodes.FindAllListNodes<SiteTemplateNode>())
			{
				if (templateNode.Value is SiteTemplateNode template)
				{
					var titleText = template.GetTitleValue();
					var templateTitle = new TitleParser(this.Site, MediaWikiNamespaces.Template, titleText);
					if (templateTitle.SimpleEquals(find))
					{
						yield return templateNode;
					}
				}
			}
		}

		/// <summary>Converts the page's <see cref="NodeCollection"/> back to text.</summary>
		/// <returns>The page text.</returns>
		public string? GetText() => WikiTextVisitor.Raw(this.Nodes);
		#endregion
	}
}