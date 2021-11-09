namespace RobinHood70.Robby.Parser
{
	//// TODO: See if the low-level parser (StackElement and derivatives) can be re-written to use a Node factory, then create a factory that aceepts Site and emits Site-specific wrappers around SiteTemplateNode and SiteLinkNode. This would vastly simplify a lot of the checking and inline conversion that's currently happening. In addition, SiteTemplateNode and SiteArgumentNode wrappers could add a settable CurrentValue property for use with the resolvers in this class.

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	/// <summary>This is a higher-level parser that works on a NodeCollection, but adds functionality to resolve magic words and templates within the context of the page.</summary>
	public class ContextualParser
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="page">The page to parse.</param>
		public ContextualParser(Page page)
			: this(page.NotNull(nameof(page)), page.Text, InclusionType.Raw, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle">title</see> the text will be on.</param>
		/// <param name="text">The text to parse.</param>
		public ContextualParser(ISimpleTitle title, string text)
			: this(title.NotNull(nameof(title)), text, InclusionType.Raw, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="page">The page to parse.</param>
		/// <param name="inclusionType">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		public ContextualParser(Page page, InclusionType inclusionType, bool strictInclusion)
			: this(page.NotNull(nameof(page)), page.Text, inclusionType, strictInclusion)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle">title</see> the text will be on.</param>
		/// <param name="text">The text to parse.</param>
		/// <param name="inclusionType">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		public ContextualParser(ISimpleTitle title, string text, InclusionType inclusionType, bool strictInclusion)
		{
			this.Context = title.NotNull(nameof(title));
			this.Nodes = new SiteNodeFactory(title.Namespace.Site).Parse(text, inclusionType, strictInclusion);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the title.</summary>
		/// <value>The title.</value>
		/// <remarks>This provides the context for resolving magic words.</remarks>
		public ISimpleTitle Context { get; set; }

		/// <summary>Gets the <see cref="IWikiNodeFactory"/> used to create new nodes.</summary>
		/// <value>The factory.</value>
		/// <remarks>This is a shortcut to <see cref="Nodes"/>.<see cref="NodeCollection.Factory">Factory</see>.</remarks>
		public IWikiNodeFactory Factory => this.Nodes.Factory;

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
			Title? catTitle = Title.Coerce(this.Site, MediaWikiNamespaces.Category, category.NotNull(nameof(category)));
			var lastCategoryIndex = -1;
			for (var i = 0; i < this.Nodes.Count; i++)
			{
				if (this.Nodes[i] is SiteLinkNode link &&
					link.TitleValue.Namespace == MediaWikiNamespaces.Category)
				{
					if (Title.FromBacklinkNode(this.Site, link).SimpleEquals(catTitle))
					{
						return false;
					}

					lastCategoryIndex = i + 1;
				}
			}

			var newCat = this.Nodes.Factory.LinkNodeFromParts(catTitle.ToString());
			if (lastCategoryIndex == -1)
			{
				if (this.Nodes.Count > 0)
				{
					this.Nodes.AddText("\n\n");
				}

				this.Nodes.Add(newCat);
			}
			else
			{
				this.Nodes.Insert(lastCategoryIndex, newCat);
			}

			return true;
		}

		/// <summary>Adds a blank line to the end of the Nodes collection.</summary>
		public void AppendLine() => this.Nodes.AddText("\n");

		/// <summary>Adds a full line of text to the end of the Nodes collection.</summary>
		/// <param name="text">The text.</param>
		/// <seealso cref="AppendText(string)"/>
		public void AppendLine(string text) => this.Nodes.AddText(text + '\n');

		/// <summary>Adds text to the end of the Nodes collection.</summary>
		/// <param name="text">The text.</param>
		/// <remarks>Adds text to the final node in the Nodes collection if it's an <see cref="ITextNode"/>; otherwise, creates a text node (via the factory) with the specified text and adds it to the Nodes collection.</remarks>
		public void AppendText(string text) => this.Nodes.AddText(text);

		/// <summary>Finds the first link that matches the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The first <see cref="SiteLinkNode"/> that matches the title provided, if found.</returns>
		/// <remarks>The text provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="ISimpleTitle"/>.</remarks>
		public SiteLinkNode? FindLink(string find) => this.FindLink(TitleFactory.FromName(this.Site, find));

		/// <summary>Finds the first link that matches the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The first <see cref="SiteLinkNode"/> that matches the title provided, if found.</returns>
		/// <remarks>As with all <see cref="ISimpleTitle"/> comparisons, only namespace and page name are checked, so trying to find <c>NS:Page</c> will match <c>NS:Page#Fragment</c> and vice versa. To match on the full title in the link, including any interwiki or fragment information, use the overload that takes an <see cref="IFullTitle"/>.</remarks>
		public SiteLinkNode? FindLink(ISimpleTitle find) => this.FindLinks(find).FirstOrDefault();

		/// <summary>Finds the first link that matches the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The first <see cref="SiteLinkNode"/> that matches the title provided, if found.</returns>
		/// <remarks>The title provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="ISimpleTitle"/>.</remarks>
		public SiteLinkNode? FindLink(IFullTitle find) => this.FindLinks(find).FirstOrDefault();

		/// <summary>Finds all links that match the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The <see cref="SiteLinkNode"/>s that match the title provided, if found.</returns>
		/// <remarks>The text provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="ISimpleTitle"/>.</remarks>
		public IEnumerable<SiteLinkNode> FindLinks(string find) => this.FindLinks(TitleFactory.FromName(this.Site, find));

		/// <summary>Finds all links that match the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The <see cref="SiteLinkNode"/>s that match the title provided, if found.</returns>
		/// <remarks>As with all <see cref="ISimpleTitle"/> comparisons, only namespace and page name are checked, so trying to find <c>NS:Page</c> will match <c>NS:Page#Fragment</c> and vice versa. To match on the full title in the link, including any interwiki or fragment information, use the overload that takes an <see cref="IFullTitle"/>.</remarks>
		public IEnumerable<SiteLinkNode> FindLinks(ISimpleTitle find)
		{
			foreach (var link in this.LinkNodes)
			{
				Title? linkTitle = Title.FromBacklinkNode(this.Site, link);
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
				FullTitle? linkTitle = FullTitle.FromBacklinkNode(this.Site, link);
				if (linkTitle.FullEquals(find))
				{
					yield return link;
				}
			}
		}

		/// <summary>Finds the first template that matches the provided title.</summary>
		/// <param name="templateName">The name of the template to find.</param>
		/// <returns>The first <see cref="SiteTemplateNode"/> that matches the title provided, if found.</returns>
		public SiteTemplateNode? FindTemplate(string templateName) => this.FindTemplates(templateName).FirstOrDefault();

		/// <summary>Finds all templates that match the provided title.</summary>
		/// <param name="templateName">The name of the template to find.</param>
		/// <returns>The templates that match the title provided, if any.</returns>
		public IEnumerable<SiteTemplateNode> FindTemplates(string templateName)
		{
			TitleFactory find = TitleFactory.FromName(this.Site, MediaWikiNamespaces.Template, templateName);
			foreach (var template in this.TemplateNodes)
			{
				var titleText = template.GetTitleText();
				TitleFactory? templateTitle = TitleFactory.FromName(this.Site, MediaWikiNamespaces.Template, titleText);
				if (templateTitle.SimpleEquals(find))
				{
					yield return template;
				}
			}
		}

		/// <summary>Replaces all current content with the content of the sections provided.</summary>
		/// <param name="sections">The new sections for the page.</param>
		public void FromSections(IEnumerable<Section> sections)
		{
			sections.ThrowNull(nameof(sections));
			this.Nodes.Clear();
			foreach (var section in sections)
			{
				if (section.Header is IHeaderNode header)
				{
					this.Nodes.Add(header);
				}

				this.Nodes.AddRange(section.Content);
			}
		}

		/// <summary>Finds the first header with the specified text.</summary>
		/// <param name="headerText">Name of the header.</param>
		/// <returns>The first header with the specified text.</returns>
		/// <remarks>This is a temporary function until HeaderNode can be rewritten to work more like other nodes (i.e., without capturing trailing whitespace).</remarks>
		public int IndexOfHeader(string headerText) => this.Nodes.FindIndex<IHeaderNode>(header => string.Equals(header.GetInnerText(true), headerText, StringComparison.Ordinal));

		/// <summary>Converts the page's <see cref="NodeCollection"/> back to text.</summary>
		/// <returns>The page text.</returns>
		public string ToRaw() => this.Nodes.ToRaw();

		/// <summary>Splits a page into its individual sections. </summary>
		/// <returns>An enumeration of the sections of the page.</returns>
		public IEnumerable<Section> ToSections()
		{
			Section? section = new(null, this.Factory);
			foreach (var node in this.Nodes)
			{
				if (node is IHeaderNode header)
				{
					if (section.Header != null || section.Content.Count > 0)
					{
						yield return section;
					}

					section = new Section(header, this.Factory);
				}
				else
				{
					section.Content.Add(node);
				}
			}

			yield return section;
		}
		#endregion
	}
}