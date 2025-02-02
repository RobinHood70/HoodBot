﻿namespace RobinHood70.Robby;
using System;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon.Parser;

/// <summary>Extensions to the WikiCommon.Parser interfaces.</summary>
public static class ParserExtensions
{
	#region ILinkNode Methods

	/// <summary>Parses the name of a template and returns it as a <see cref="Title"/>.</summary>
	/// <param name="template">The template to get the title for.</param>
	/// <param name="site">The site the title is from.</param>
	/// <returns>The title.</returns>
	public static Title GetTitle(this ITemplateNode template, Site site)
	{
		ArgumentNullException.ThrowIfNull(template);
		ArgumentNullException.ThrowIfNull(site);
		var titleText = template.TitleNodes.ToValue();
		return TitleFactory.FromTemplate(site, titleText);
	}
	#endregion

	#region ILinkNode Methods

	/// <summary>Parses the title and returns the trimmed value.</summary>
	/// <param name="link">The backlink to get the title for.</param>
	/// <param name="site">The site the title is from.</param>
	/// <returns>The title.</returns>
	public static SiteLink GetLink(this ILinkNode link, Site site)
	{
		ArgumentNullException.ThrowIfNull(link);
		ArgumentNullException.ThrowIfNull(site);
		return SiteLink.FromLinkNode(site, link);
	}

	/// <summary>Parses the link and returns the title portion as a <see cref="Title"/>.</summary>
	/// <param name="link">The link to get the title for.</param>
	/// <param name="site">The site the link is from.</param>
	/// <returns>The title.</returns>
	public static Title GetTitle(this ILinkNode link, Site site)
	{
		ArgumentNullException.ThrowIfNull(link);
		ArgumentNullException.ThrowIfNull(site);
		var titleText = link.TitleNodes.ToValue();
		return TitleFactory.FromUnvalidated(site, titleText).Title;
	}
	#endregion
}