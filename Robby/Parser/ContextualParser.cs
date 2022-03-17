﻿namespace RobinHood70.Robby.Parser
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
	public class ContextualParser : NodeCollection
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="page">The page to parse.</param>
		public ContextualParser(Page page)
			: this(page.NotNull(nameof(page)), page.Text, InclusionType.Raw, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ContextualParser"/> class.</summary>
		/// <param name="title">The <see cref="SimpleTitle">title</see> the text will be on.</param>
		/// <param name="text">The text to parse.</param>
		public ContextualParser(Page title, string text)
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
		/// <param name="title">The <see cref="SimpleTitle">title</see> the text will be on.</param>
		/// <param name="text">The text to parse.</param>
		/// <param name="inclusionType">The inclusion type for the text. <see langword="true"/> to return text as if transcluded to another page; <see langword="false"/> to return local text only; <see langword="null"/> to return all text. In each case, any ignored text will be wrapped in an IgnoreNode.</param>
		/// <param name="strictInclusion"><see langword="true"/> if the output should exclude IgnoreNodes; otherwise <see langword="false"/>.</param>
		public ContextualParser(Page title, string text, InclusionType inclusionType, bool strictInclusion)
			: base(new SiteNodeFactory(title.NotNull(nameof(title)).Namespace.Site))
		{
			this.Page = title;
			this.Factory.ParseInto(this, text, inclusionType, strictInclusion);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the title.</summary>
		/// <value>The title.</value>
		/// <remarks>This provides the context for resolving magic words.</remarks>
		public Page Page { get; set; }

		/// <summary>Gets a set of functions to evaluate magic words (e.g., <c>{{PAGENAME}}</c>) and resolve them into meaningful values (NOT IMPLEMENTED).</summary>
		/// <value>The magic word resolvers.</value>
		public IDictionary<string, Func<string>> MagicWordResolvers { get; } = new Dictionary<string, Func<string>>(StringComparer.Ordinal);

		/// <summary>Gets a set parameter (e.g., <c>{{{1|}}}</c>) names and the values to be used when resolving them (NOT IMPLEMENTED).</summary>
		/// <value>The parameters.</value>
		public IDictionary<string, string> Parameters { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

		/// <summary>Gets the current Site from the Title.</summary>
		/// <value>The site.</value>
		/// <remarks>This property is a direct link to Title and will therefore change if the Title's Site does. Changing Sites within a session may produce unexpected results.</remarks>
		public Site Site => this.Page.Namespace.Site;

		/// <summary>Gets a set of functions to evaluate templates (e.g., <c>{{PAGENAME}}</c>) and resolve them into meaningful values (NOT IMPLEMENTED).</summary>
		/// <value>The template resolvers.</value>
		public IDictionary<string, Func<string>> TemplateResolvers { get; } = new Dictionary<string, Func<string>>(StringComparer.Ordinal);
		#endregion

		#region Public Methods

		/// <summary>Adds a category to the page.</summary>
		/// <param name="category">The category to add.</param>
		/// <param name="newLineBefore">Whether to add a new line before the category.</param>
		/// <returns><see langword="true"/> if the category was added to the page; <see langword="false"/> if was already on the page.</returns>
		/// <remarks>The category will be added after the last category found on the page, or at the end of the page (preceded by two newlines) if no categories were found.</remarks>
		public bool AddCategory(string category, bool newLineBefore)
		{
			Title catTitle = Title.FromUnvalidated(this.Site, MediaWikiNamespaces.Category, category.NotNull(nameof(category)));
			var lastCategoryIndex = -1;
			for (var i = 0; i < this.Count; i++)
			{
				if (this[i] is SiteLinkNode link &&
					link.TitleValue.Namespace == MediaWikiNamespaces.Category)
				{
					if (Title.FromBacklinkNode(this.Site, link).SimpleEquals(catTitle))
					{
						return false;
					}

					lastCategoryIndex = i + 1;
				}
			}

			var newCat = this.Factory.LinkNodeFromParts(catTitle.ToString());
			if (lastCategoryIndex == -1)
			{
				if (this.Count > 0)
				{
					// makes sure two LFs are added no matter what, since newLineBefore adds a LF already
					this.AddText(newLineBefore ? "\n" : "\n\n");
				}

				lastCategoryIndex = this.Count;
				//// this.Nodes.Add(newCat);
			}

			if (newLineBefore && lastCategoryIndex > 0)
			{
				this.Insert(lastCategoryIndex, this.Factory.TextNode("\n"));
				lastCategoryIndex++;
			}

			this.Insert(lastCategoryIndex, newCat);
			return true;
		}

		/// <summary>Finds the first link that matches the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The first <see cref="SiteLinkNode"/> that matches the title provided, if found.</returns>
		/// <remarks>The text provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="SimpleTitle"/>.</remarks>
		public SiteLinkNode? FindSiteLink(string find) => this.FindSiteLink(Title.FromUnvalidated(this.Site, find));

		/// <summary>Finds the first link that matches the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The first <see cref="SiteLinkNode"/> that matches the title provided, if found.</returns>
		/// <remarks>As with all <see cref="SimpleTitle"/> comparisons, only namespace and page name are checked, so trying to find <c>NS:Page</c> will match <c>NS:Page#Fragment</c> and vice versa. To match on the full title in the link, including any interwiki or fragment information, use the overload that takes an <see cref="IFullTitle"/>.</remarks>
		public SiteLinkNode? FindSiteLink(SimpleTitle find) => this.FindSiteLinks(find).FirstOrDefault();

		/// <summary>Finds the first link that matches the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The first <see cref="SiteLinkNode"/> that matches the title provided, if found.</returns>
		/// <remarks>The title provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="SimpleTitle"/>.</remarks>
		public SiteLinkNode? FindSiteLink(IFullTitle find) => this.FindSiteLinks(find).FirstOrDefault();

		/// <summary>Finds all links that match the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The <see cref="SiteLinkNode"/>s that match the title provided, if found.</returns>
		/// <remarks>The text provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="SimpleTitle"/>.</remarks>
		public IEnumerable<SiteLinkNode> FindSiteLinks(string find) => this.FindSiteLinks(Title.FromUnvalidated(this.Site, find));

		/// <summary>Finds all links that match the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The <see cref="SiteLinkNode"/>s that match the title provided, if found.</returns>
		/// <remarks>As with all <see cref="SimpleTitle"/> comparisons, only namespace and page name are checked, so trying to find <c>NS:Page</c> will match <c>NS:Page#Fragment</c> and vice versa. To match on the full title in the link, including any interwiki or fragment information, use the overload that takes an <see cref="IFullTitle"/>.</remarks>
		public IEnumerable<SiteLinkNode> FindSiteLinks(SimpleTitle find)
		{
			foreach (var link in this.LinkNodes)
			{
				Title? linkTitle = Title.FromBacklinkNode(this.Site, link);
				if (link is SiteLinkNode siteLink && linkTitle.SimpleEquals(find))
				{
					yield return siteLink;
				}
			}
		}

		/// <summary>Finds all links that match the provided title.</summary>
		/// <param name="find">The title to find.</param>
		/// <returns>The <see cref="SiteLinkNode"/>s that match the title provided, if found.</returns>
		/// <remarks>The title provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="SimpleTitle"/>.</remarks>
		public IEnumerable<SiteLinkNode> FindSiteLinks(IFullTitle find)
		{
			foreach (var link in this.LinkNodes)
			{
				FullTitle? linkTitle = FullTitle.FromBacklinkNode(this.Site, link);
				if (link is SiteLinkNode siteLink && linkTitle.FullEquals(find))
				{
					yield return siteLink;
				}
			}
		}

		/// <summary>Finds the first template that matches the provided title.</summary>
		/// <param name="templateName">The name of the template to find.</param>
		/// <returns>The first <see cref="SiteTemplateNode"/> that matches the title provided, if found.</returns>
		public SiteTemplateNode? FindSiteTemplate(string templateName) => this.FindSiteTemplates(templateName).FirstOrDefault();

		/// <summary>Finds all templates that match the provided title.</summary>
		/// <param name="templateName">The name of the template to find.</param>
		/// <returns>The templates that match the title provided, if any.</returns>
		public IEnumerable<SiteTemplateNode> FindSiteTemplates(string templateName)
		{
			Title find = Title.FromUnvalidated(this.Site, MediaWikiNamespaces.Template, templateName);
			foreach (var template in this.TemplateNodes)
			{
				var titleText = template.GetTitleText();
				Title templateTitle = Title.FromUnvalidated(this.Site, MediaWikiNamespaces.Template, titleText);
				if (template is SiteTemplateNode siteTemplate && templateTitle.SimpleEquals(find))
				{
					yield return siteTemplate;
				}
			}
		}

		/// <summary>Updates the <see cref="Page"/>'s <see cref="Page.Text">text</see> to the parser's contents.</summary>
		public void UpdatePage() => this.Page.Text = this.ToRaw();
		#endregion
	}
}