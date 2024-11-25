namespace RobinHood70.Robby.Parser;
using System;
using System.Collections.Generic;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

/// <summary>Extends WikiNodeCollection to include <see cref="Title"/>-based methods, either directly or indirectly.</summary>
public static class ParserHelpers
{
	#region Public Methods

	/// <summary>Adds a category to the page.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="site">The site being worked with.</param>
	/// <param name="category">The category to add.</param>
	/// <param name="newLineBefore">Whether to add a new line before the category.</param>
	/// <returns><see langword="true"/> if the category was added to the page; <see langword="false"/> if was already on the page.</returns>
	/// <remarks>The category will be added after the last category found on the page, or at the end of the page (preceded by two newlines) if no categories were found.</remarks>
	public static bool AddCategory(this WikiNodeCollection nodes, Site site, string category, bool newLineBefore)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(category);
		var lastCategoryIndex = -1;
		for (var i = 0; i < nodes.Count; i++)
		{
			if (nodes[i] is ILinkNode link &&
				TitleFactory.FromBacklinkNode(site, link).Title is var title &&
				title.Namespace == MediaWikiNamespaces.Category)
			{
				if (title.PageNameEquals(category))
				{
					return false;
				}

				lastCategoryIndex = i;
			}
		}

		lastCategoryIndex++;
		if (lastCategoryIndex == 0)
		{
			if (nodes.Count > 0)
			{
				// makes sure two LFs are added no matter what, since newLineBefore adds a LF already
				nodes.AddText(newLineBefore ? "\n" : "\n\n");
			}

			lastCategoryIndex = nodes.Count;
			//// nodes.Nodes.Add(newCat);
		}

		if (newLineBefore && lastCategoryIndex > 0)
		{
			nodes.Insert(lastCategoryIndex, nodes.Factory.TextNode("\n"));
			lastCategoryIndex++;
		}

		nodes.Insert(lastCategoryIndex, nodes.Factory.LinkNodeFromParts(site[MediaWikiNamespaces.Category].Name + ':' + category));
		return true;
	}

	/// <summary>Finds the first link that matches the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="site">The site being worked with.</param>
	/// <param name="find">The title to find.</param>
	/// <returns>The first <see cref="ILinkNode"/> that matches the title provided, if found.</returns>
	/// <remarks>The text provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="Title"/>.</remarks>
	public static ILinkNode? FindLink(this WikiNodeCollection nodes, Site site, string find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(find);
		var title = TitleFactory.FromUnvalidated(site, find);
		return (title.Fragment is null && title.Interwiki is null)
			? nodes.FindLink(title.Title)
			: nodes.FindLink(title.ToFullTitle());
	}

	/// <summary>Finds the first link that matches the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="find">The title to find.</param>
	/// <returns>The first <see cref="ILinkNode"/> that matches the title provided, if found.</returns>
	/// <remarks>As with all <see cref="Title"/> comparisons, only namespace and page name are checked, so trying to find <c>NS:Page</c> will match <c>NS:Page#Fragment</c> and vice versa. To match on the full title in the link, including any interwiki or fragment information, use the overload that takes an <see cref="IFullTitle"/>.</remarks>
	public static ILinkNode? FindLink(this WikiNodeCollection nodes, Title find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(find);
		return find is null
			? null
			: nodes.FindLink(link => link.GetTitle(find.Site) == find);
	}

	/// <summary>Finds the first link that matches the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="find">The title to find.</param>
	/// <returns>The first <see cref="ILinkNode"/> that matches the title provided, if found.</returns>
	/// <remarks>The title provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="Title"/>.</remarks>
	public static ILinkNode? FindLink(this WikiNodeCollection nodes, IFullTitle find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(find);
		return find is null
			? null
			: nodes.FindLink(link => TitleFactory.FromBacklinkNode(find.Title.Site, link).FullEquals(find));
	}

	/// <summary>Finds all links that match the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="site">The site being worked with.</param>
	/// <param name="find">The title to find.</param>
	/// <returns>The <see cref="ILinkNode"/>s that match the title provided, if found.</returns>
	/// <remarks>The text provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="Title"/>.</remarks>
	public static IEnumerable<ILinkNode> FindLinks(this WikiNodeCollection nodes, Site site, string find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(find);
		var title = TitleFactory.FromUnvalidated(site, find);
		return (title.Fragment is null && title.Interwiki is null)
			? nodes.FindLinks(title.Title)
			: nodes.FindLinks(title.ToFullTitle());
	}

	/// <summary>Finds all links that match the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="find">The title to find.</param>
	/// <returns>The <see cref="ILinkNode"/>s that match the title provided, if found.</returns>
	/// <remarks>As with all <see cref="Title"/> comparisons, only namespace and page name are checked, so trying to find <c>NS:Page</c> will match <c>NS:Page#Fragment</c> and vice versa. To match on the full title in the link, including any interwiki or fragment information, use the overload that takes an <see cref="IFullTitle"/>.</remarks>
	public static IEnumerable<ILinkNode> FindLinks(this WikiNodeCollection nodes, Title find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(find);
		return find is null
			? []
			: nodes.FindLinks(link => link.GetTitle(find.Site) == find);
	}

	/// <summary>Finds all links that match the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="find">The title to find.</param>
	/// <returns>The <see cref="ILinkNode"/>s that match the title provided, if found.</returns>
	/// <remarks>The title provided will be evaluated as an <see cref="IFullTitle"/>, so trying to find <c>NS:Page</c> will not match <c>NS:Page#Fragment</c> and vice versa. To only match on the root of the link, use the overload that takes an <see cref="Title"/>.</remarks>
	public static IEnumerable<ILinkNode> FindLinks(this WikiNodeCollection nodes, IFullTitle find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(find);
		return find is null
			? []
			: nodes.FindLinks(link => TitleFactory.FromBacklinkNode(find.Title.Site, link).FullEquals(find));
	}

	/// <summary>Finds the first template that matches the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="site">The site being worked with.</param>
	/// <param name="find">The name of the template to find.</param>
	/// <returns>The first <see cref="ITemplateNode"/> that matches the title provided, if found.</returns>
	public static ITemplateNode? FindTemplate(this WikiNodeCollection nodes, Site site, string find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(find);
		return nodes.FindTemplate(TitleFactory.FromTemplate(site, find));
	}

	/// <summary>Finds all templates that match the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="find">The template to find.</param>
	/// <returns>The templates that match the title provided, if any.</returns>
	public static ITemplateNode? FindTemplate(this WikiNodeCollection nodes, Title find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(find);
		return find is null
			? null
			: nodes.FindTemplate(link => link.GetTitle(find.Site) == find);
	}

	/// <summary>Finds all templates that match the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="site">The site being worked with.</param>
	/// <param name="find">The template to find.</param>
	/// <returns>The templates that match the title provided, if any.</returns>
	public static IEnumerable<ITemplateNode> FindTemplates(this WikiNodeCollection nodes, Site site, string find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(find);
		return nodes.FindTemplates(TitleFactory.FromTemplate(site, find));
	}

	/// <summary>Finds all templates that match the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="find">The template to find.</param>
	/// <returns>The templates that match the title provided, if any.</returns>
	public static IEnumerable<ITemplateNode> FindTemplates(this WikiNodeCollection nodes, Title find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(find);
		return nodes.FindTemplates(template => template.GetTitle(find.Site) == find);
	}

	/// <summary>Finds all templates that match the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="site">The site being worked with.</param>
	/// <param name="find">The templates to find.</param>
	/// <returns>The templates that match the provided titles, if any.</returns>
	public static IEnumerable<ITemplateNode> FindTemplates(this WikiNodeCollection nodes, Site site, IEnumerable<string> find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(find);
		return nodes.FindTemplates(new TitleCollection(site, MediaWikiNamespaces.Template, find));
	}

	/// <summary>Finds all templates that match the provided title.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="find">The templates to find.</param>
	/// <returns>The templates that match the title provided, if any.</returns>
	public static IEnumerable<ITemplateNode> FindTemplates(this WikiNodeCollection nodes, IList<Title> find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(find);
		return find.Count == 0
			? []
			: FindTemplatesInternal(nodes, find);

		IEnumerable<ITemplateNode> FindTemplatesInternal(WikiNodeCollection nodes, IList<Title> find)
		{
			var site = find[0].Site;
			return nodes.FindTemplates(
				templateNode =>
				new TitleCollection(site, find).Contains(templateNode.GetTitle(site)));
		}
	}

	/// <summary>Removes all instances of a template and, if appropriate, pulls up any following text to the template's former position.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="site">The site being worked with.</param>
	/// <param name="find">The name of the template.</param>
	public static void RemoveTemplates(this WikiNodeCollection nodes, Site site, string find)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(find);
		nodes.RemoveTemplates(TitleFactory.FromTemplate(site, find));
	}

	/// <summary>Removes all instances of a template and, if appropriate, pulls up any following text to the template's former position.</summary>
	/// <param name="nodes">The node collection to work on.</param>
	/// <param name="title">The title of the template.</param>
	public static void RemoveTemplates(this WikiNodeCollection nodes, Title title)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(title);
		for (var i = nodes.Count - 1; i >= 0; i--)
		{
			var node = nodes[i];
			if (node is ITemplateNode template && template.GetTitle(title.Site) == title)
			{
				nodes.RemoveAt(i);
				var afterNewLine = i == 0 ||
					(nodes[i - 1] is ITextNode textBefore &&
					textBefore.Text.Length > 0 &&
					textBefore.Text[^1] == '\n');
				if (afterNewLine &&
					i < nodes.Count &&
					nodes[i] is ITextNode textAfter)
				{
					textAfter.Text = textAfter.Text.TrimStart();
					if (textAfter.Text.Length == 0)
					{
						nodes.RemoveAt(i);
					}
				}
			}
		}
	}
	#endregion
}