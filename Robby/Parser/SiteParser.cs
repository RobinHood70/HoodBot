namespace RobinHood70.Robby.Parser;

//// TODO: See if the low-level parser (StackElement and derivatives) can be re-written to use a Node factory, then create a factory that aceepts Site and emits Site-specific wrappers around ITemplateNode and ILinkNode. This would vastly simplify a lot of the checking and inline conversion that's currently happening. In addition, ITemplateNode and SiteArgumentNode wrappers could add a settable CurrentValue property for use with the resolvers in this class.

using System;
using System.Collections.Generic;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon.Parser;
using RobinHood70.WikiCommon.Parser.Basic;

/// <summary>This is a higher-level parser that works on a WikiNodeCollection, but adds functionality to resolve magic words and templates within the context of the page.</summary>
public class SiteParser : WikiNodeCollection, ITitle
{
	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="SiteParser"/> class.</summary>
	/// <param name="page">The page to parse.</param>
	public SiteParser(Page page)
		: this(page, page?.Text, InclusionType.Raw, false)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="SiteParser"/> class.</summary>
	/// <param name="page">The <see cref="Title">title</see> the text will be on.</param>
	/// <param name="text">The text to parse.</param>
	public SiteParser(Page page, string? text)
		: this(page, text, InclusionType.Raw, false)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="SiteParser"/> class.</summary>
	/// <param name="page">The page to parse.</param>
	/// <param name="inclusionType">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
	/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
	public SiteParser(Page page, InclusionType inclusionType, bool strictInclusion)
		: this(page, page?.Text, inclusionType, strictInclusion)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="SiteParser"/> class.</summary>
	/// <param name="page">The <see cref="Title">title</see> the text will be on.</param>
	/// <param name="text">The text to parse. Null values will be treated as empty strings.</param>
	/// <param name="inclusionType">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
	/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
	public SiteParser(Page page, string? text, InclusionType inclusionType, bool strictInclusion)
		: base(new WikiNodeFactory())
	{
		ArgumentNullException.ThrowIfNull(page);
		this.Page = page;
		this.Site = page.Site;
		var nodes = this.Factory.Parse(text, inclusionType, strictInclusion);
		this.AddRange(nodes);
	}
	#endregion

	#region Public Properties

	/// <summary>Gets a set of functions to evaluate magic words (e.g., <c>{{PAGENAME}}</c>) and resolve them into meaningful values (NOT IMPLEMENTED).</summary>
	/// <value>The magic word resolvers.</value>
	public IDictionary<string, Func<string>> MagicWordResolvers { get; } = new Dictionary<string, Func<string>>(StringComparer.Ordinal);

	/// <summary>Gets or sets the title.</summary>
	/// <value>The title.</value>
	/// <remarks>This provides the context for resolving magic words.</remarks>
	public Page Page { get; set; }

	/// <summary>Gets a set parameter (e.g., <c>{{{1|}}}</c>) names and the values to be used when resolving them (NOT IMPLEMENTED).</summary>
	/// <value>The parameters.</value>
	public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

	/// <summary>Gets the current Site from the Title.</summary>
	/// <value>The site.</value>
	/// <remarks>This property is a direct link to Title and will therefore change if the Title's Site does. Changing Sites within a session may produce unexpected results.</remarks>
	public Site Site { get; }

	/// <summary>Gets a set of functions to evaluate templates (e.g., <c>{{PAGENAME}}</c>) and resolve them into meaningful values (NOT IMPLEMENTED).</summary>
	/// <value>The template resolvers.</value>
	public IDictionary<string, Func<string>> TemplateResolvers { get; } = new Dictionary<string, Func<string>>(StringComparer.Ordinal);

	/// <inheritdoc/>
	public Title Title => this.Page.Title;
	#endregion

	#region Public Methods

	/// <summary>Adds a category to the page.</summary>
	/// <param name="category">The category to add.</param>
	/// <param name="newLineBefore">Whether to add a new line before the category.</param>
	/// <returns><see langword="true"/> if the category was added to the page; <see langword="false"/> if was already on the page.</returns>
	/// <remarks>The category will be added after the last category found on the page, or at the end of the page (preceded by two newlines) if no categories were found.</remarks>
	public bool AddCategory(string category, bool newLineBefore) => this.AddCategory(this.Site, category, newLineBefore);

	/// <summary>Finds the first link that matches the provided title.</summary>
	/// <param name="find">The title to find.</param>
	/// <returns>The first <see cref="ILinkNode"/> that matches the title provided, if found.</returns>
	/// <remarks>The text provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="Title"/>.</remarks>
	public ILinkNode? FindLink(string find) => this.FindLink(this.Site, find);

	/// <summary>Finds all links that match the provided title.</summary>
	/// <param name="find">The title to find.</param>
	/// <returns>The <see cref="ILinkNode"/>s that match the title provided, if found.</returns>
	/// <remarks>The text provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="Title"/>.</remarks>
	public IEnumerable<ILinkNode> FindLinks(string find) => this.FindLinks(this.Site, find);

	/// <summary>Finds the first template that matches the provided title.</summary>
	/// <param name="find">The name of the template to find.</param>
	/// <returns>The first <see cref="ITemplateNode"/> that matches the title provided, if found.</returns>
	public ITemplateNode? FindTemplate(string find) => this.FindTemplate(this.Site, find);

	/// <summary>Finds all templates that match the provided title.</summary>
	/// <param name="find">The template to find.</param>
	/// <returns>The templates that match the title provided, if any.</returns>
	public IEnumerable<ITemplateNode> FindTemplates(string find) => this.FindTemplates(this.Site, find);

	/// <summary>Finds all templates that match the provided title.</summary>
	/// <param name="find">The templates to find.</param>
	/// <returns>The templates that match the provided titles, if any.</returns>
	public IEnumerable<ITemplateNode> FindTemplates(IEnumerable<string> find) => this.FindTemplates(this.Site, find);

	/// <summary>Parses the given text for use with methods expecting <see cref="IWikiNode"/>s.</summary>
	/// <param name="text">The text to parse.</param>
	/// <returns>A new WikiNodeCollection created from the text.</returns>
	public IList<IWikiNode> Parse(string? text) => this.Parse(this.Site, text);

	/// <summary>Removes all instances of a template and, if appropriate, pulls up any following text to the template's former position.</summary>
	/// <param name="find">The name of the template.</param>
	public void RemoveTemplates(string find) => this.RemoveTemplates(this.Site, find);

	/// <summary>Reparses the existing page text with new inclusion parameters.</summary>
	/// <param name="inclusionType">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
	/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
	/// <remarks>This can be used either to reparse the same text with different inclusion parameters or to cause a totally new parse <see langword="if"/>the page text has been altered directly.</remarks>
	public void ReparsePageText(InclusionType inclusionType, bool strictInclusion)
	{
		this.Clear();
		var newNodes = this.Factory.Parse(this.Page.Text, inclusionType, strictInclusion);
		this.AddRange(newNodes);
	}

	/// <summary>Updates the <see cref="Page"/>'s <see cref="Page.Text">text</see> to the parser's contents.</summary>
	public void UpdatePage() => this.Page.Text = this.ToRaw();
	#endregion
}