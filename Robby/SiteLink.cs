﻿namespace RobinHood70.Robby;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Properties;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;
using RobinHood70.WikiCommon.Parser.Basic;

#region Public Enumerations

/// <summary>The format to use for the link text.</summary>
public enum LinkFormat
{
	/// <summary>Forces link format if the Title represented is in Category or File space.</summary>
	ForcedLink,

	/// <summary>Plain link with no text.</summary>
	Plain,

	/// <summary>Link text should follow "pipe trick" rules.</summary>
	PipeTrick,

	/// <summary>Link text should strip paranthetical text only.</summary>
	LabelName
}

/// <summary>The parameter type of a given value.</summary>
public enum ParameterType
{
	/// <summary>HTML alt text parameter.</summary>
	Alternate,

	/// <summary>Border parameter.</summary>
	Border,

	/// <summary>Caption parameter.</summary>
	Caption,

	/// <summary>Class parameter.</summary>
	Class,

	/// <summary>Format parameter.</summary>
	Format,

	/// <summary>Horizontal alignment parameter.</summary>
	Halign,

	/// <summary>Language parameter.</summary>
	Language,

	/// <summary>Link parameter.</summary>
	Link,

	/// <summary>Page parameter.</summary>
	Page,

	/// <summary>Size parameter.</summary>
	Size,

	/// <summary>Upright parameter.</summary>
	Upright,

	/// <summary>Vertical alignment parameter.</summary>
	Valign,
}
#endregion

/// <summary>Represents a link with site-specific Title information and parameters in the site's language.</summary>
public class SiteLink : ILinkTitle
{
	#region Static Fields
	private static readonly Dictionary<string, ParameterType> DirectValues = new(StringComparer.Ordinal);
	private static readonly List<(ParameterType ParameterType, string Before, string After)> ImageParameterInfo = [];
	private static readonly Dictionary<string, ParameterType> ImageWords = new(StringComparer.Ordinal)
	{
		["img_baseline"] = ParameterType.Valign, // no params
		["img_sub"] = ParameterType.Valign, // no params
		["img_super"] = ParameterType.Valign, // no params
		["img_top"] = ParameterType.Valign, // no params
		["img_text_top"] = ParameterType.Valign, // no params
		["img_middle"] = ParameterType.Valign, // no params
		["img_bottom"] = ParameterType.Valign, // no params
		["img_text_bottom"] = ParameterType.Valign, // no params
		["img_alt"] = ParameterType.Alternate, // has param
		["img_border"] = ParameterType.Border, // no params
		["img_class"] = ParameterType.Class, // has param
		["img_framed"] = ParameterType.Format, // no params
		["img_frameless"] = ParameterType.Format, // no params
		["img_thumbnail"] = ParameterType.Format, // no params
		["img_manualthumb"] = ParameterType.Format, // has param (if no match for set direct value, set thumb=value)
		["img_lang"] = ParameterType.Language, // has param
		["img_link"] = ParameterType.Link, // has param
		["img_right"] = ParameterType.Halign, // no params
		["img_left"] = ParameterType.Halign, // no params
		["img_center"] = ParameterType.Halign, // no params
		["img_none"] = ParameterType.Halign, // no params
		["img_page"] = ParameterType.Page, // has param
		["img_width"] = ParameterType.Size, // has param
		["img_upright"] = ParameterType.Upright, // optional param (use 0 = none)
	};

	private static readonly InvalidOperationException NonNumeric = new(Resources.SizeInvalid);
	private static readonly Dictionary<ParameterType, string> PreferredWords = [];
	private static readonly char[] SplitX = ['x'];
	#endregion

	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class.</summary>
	/// <param name="title">The <see cref="TitleFactory"/> with the desired information.</param>
	public SiteLink(ILinkTitle title)
		: this((IFullTitle)title)
	{
		this.Coerced = title.Coerced;
		this.ForcedInterwikiLink = title.ForcedInterwikiLink;
		this.ForcedNamespaceLink = title.ForcedNamespaceLink;
		InitializeImageInfo(this.Title.Site);
	}

	/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class.</summary>
	/// <param name="title">The <see cref="IFullTitle"/> to downcast.</param>
	public SiteLink(IFullTitle title)
	{
		ArgumentNullException.ThrowIfNull(title);
		this.Title = title.Title;
		this.Fragment = title.Fragment;
		this.Interwiki = title.Interwiki;
	}

	/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class.</summary>
	/// <param name="title">The <see cref="Title"/> to downcast.</param>
	public SiteLink(Title title)
	{
		this.Title = title;
	}
	#endregion

	#region Public Properties

	/// <summary>Gets or sets the alt text for the image.</summary>
	/// <value>The alt text.</value>
	public string? AltText
	{
		get => this.GetValue(ParameterType.Alternate);
		set => this.SetParameterValue(ParameterType.Alternate, value);
	}

	/// <summary>Gets or sets a value indicating whether a border should be displayed.</summary>
	/// <value><see langword="true"/> to display a border; otherwise, <see langword="false"/>.</value>
	/// <remarks>Use this property to insert the default border text in the wiki's language.</remarks>
	public bool Border
	{
		get => this.GetValue(ParameterType.Border) != null;
		set => this.SetDirectValue(ParameterType.Border, value ? PreferredWords[ParameterType.Border] : null);
	}

	/// <summary>Gets or sets the class for the image.</summary>
	/// <value>The class for the image.</value>
	public string? Class
	{
		get => this.GetValue(ParameterType.Class);
		set => this.SetParameterValue(ParameterType.Class, value);
	}

	/// <inheritdoc/>
	public bool Coerced { get; private set; }

	/// <summary>Gets or sets the image dimensions directly.</summary>
	/// <value>The image dimensions.</value>
	/// <remarks>Setting this option will remove any <see cref="Upright"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
	public string? Dimensions
	{
		get => this.GetValue(ParameterType.Size);
		set
		{
			this.Parameters.Remove(ParameterType.Upright);
			this.SetParameterValue(ParameterType.Size, value);
		}
	}

	/// <inheritdoc/>
	public bool ForcedInterwikiLink { get; private set; }

	/// <inheritdoc/>
	public bool ForcedNamespaceLink { get; private set; }

	/// <summary>Gets or sets the format (i.e., thumbnail, frame, frameless).</summary>
	/// <value>The format.</value>
	public string? Format
	{
		get => this.GetValue(ParameterType.Format);
		set
		{
			if (!this.SetDirectValue(ParameterType.Format, value))
			{
				// If the value is recognized via SetDirectValue, use it. Otherwise, this should find the only option with a parameter (manualthumb). If there end up being more options with parameters in the future, something else will need to be done here.
				this.SetParameterValue(ParameterType.Format, value);
			}
		}
	}

	/// <summary>Gets or sets the title's fragment (the section or ID to scroll to).</summary>
	/// <value>The fragment.</value>
	public string? Fragment { get; set; }

	/// <summary>Gets or sets the image height.</summary>
	/// <value>The image height.</value>
	/// <remarks>Setting this option will remove any <see cref="Upright"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
	public int Height
	{
		get => this.GetSize().Height;
		set => this.SetSize(value, this.Width);
	}

	/// <summary>Gets or sets the image's horizontal alignment.</summary>
	/// <value>The image's horizontal alignment.</value>
	public string? HorizontalAlignment
	{
		get => this.GetValue(ParameterType.Halign);
		set => this.SetDirectValue(ParameterType.Halign, value);
	}

	/// <summary>Gets or sets the interwiki prefix.</summary>
	/// <value>The interwiki prefix.</value>
	public InterwikiEntry? Interwiki { get; set; }

	/// <summary>Gets a value indicating whether this instance is identical to the local wiki.</summary>
	/// <value><see langword="true"/> if this instance is local wiki; otherwise, <see langword="false"/>.</value>
	public bool IsLocal => this.Interwiki is null || this.Interwiki.LocalWiki;

	/// <summary>Gets or sets the image's language, for image formats that are language-aware (e.g., SVG).</summary>
	/// <value>The image language.</value>
	public string? Language
	{
		get => this.GetValue(ParameterType.Language);
		set => this.SetParameterValue(ParameterType.Language, value);
	}

	/// <summary>Gets or sets the link for the image.</summary>
	/// <value>The link for the image.</value>
	public string? Link
	{
		get => this.GetValue(ParameterType.Link);
		set => this.SetParameterValue(ParameterType.Link, value);
	}

	/// <summary>Gets the original text of the link, in case we need to make display text out of it.</summary>
	/// <value>The original link.</value>
	/// <remarks>This will normally only be null if the title was created from scratch using one of the constructors.</remarks>
	public string? OriginalTitle { get; private set; }

	/// <summary>Gets or sets the <see cref="Robby.Page"/> value as an integer.</summary>
	/// <value>The page value.</value>
	public int? Page
	{
		get => int.Parse(this.GetValue(ParameterType.Page) ?? "0", CultureInfo.InvariantCulture);
		set => this.SetParameterValue(ParameterType.Page, value?.ToStringInvariant());
	}

	/// <summary>Gets the raw parameter information.</summary>
	/// <value>The parameters.</value>
	/// <remarks>Parameters can be used to change low-level information, such as spacing around the parameters or choosing an alternate language for language-specific parameters. Note that these are not checked in any way, and incorrect data could cause unexpected behaviour or errors.</remarks>
	public IDictionary<ParameterType, EmbeddedValue> Parameters { get; } = new Dictionary<ParameterType, EmbeddedValue>();

	/// <summary>Gets a value indicating whether any overlapping parameters were dropped during initial parsing (e.g., left|right, multiple captions).</summary>
	/// <value><see langword="true"/> if parameters were dropped; otherwise, <see langword="false"/>.</value>
	public bool ParametersDropped { get; private set; }

	/// <summary>Gets or sets the display text (i.e., the value to the right of the pipe). For categories, this is the sortkey; for images, this is the caption.</summary>
	public string? Text
	{
		get => this.GetValue(ParameterType.Caption);
		set => this.SetDirectValue(ParameterType.Caption, value);
	}

	/// <inheritdoc/>
	public Title Title { get; }

	/// <summary>Gets or sets the whitespace after the title.</summary>
	/// <value>The whitespace after the title.</value>
	public string TitleWhitespaceAfter { get; set; } = string.Empty;

	/// <summary>Gets or sets the whitespace before the title.</summary>
	/// <value>The whitespace before the title.</value>
	public string TitleWhitespaceBefore { get; set; } = string.Empty;

	/// <summary>Gets or sets the upright value as a number.</summary>
	/// <value>The upright value.</value>
	/// <remarks>Setting this option will remove any <see cref="Dimensions"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
	public double? Upright
	{
		get => this.GetValue(ParameterType.Upright) switch
		{
			null => null,
			string tv when tv.TrimEnd().Length == 0 => 1,
			string tv when double.TryParse(tv, NumberStyles.Any, this.Title.Site.Culture, out var retval) => retval,
			_ => double.NaN
		};

		set
		{
			if (value == null)
			{
				this.Parameters.Remove(ParameterType.Upright);
			}
			else if (!double.IsNegative(value.Value))
			{
				this.Parameters.Remove(ParameterType.Size);
				if (value == 0)
				{
					this.SetDirectValue(ParameterType.Upright, PreferredWords[ParameterType.Upright]);
				}
				else
				{
					this.SetParameterValue(ParameterType.Upright, value.ToStringInvariant());
				}
			}
		}
	}

	/// <summary>Gets or sets the image width.</summary>
	/// <value>The image width.</value>
	/// <remarks>Setting this option will remove any <see cref="Upright"/> parameter, and vice versa, since they are mutually exclusive. Both may be set simultaneously, however, if the link is parsed from existing text. In that case, if either is altered, the other will be removed.</remarks>
	public int Width
	{
		get => this.GetSize().Width;
		set => this.SetSize(value, this.Height);
	}

	/// <summary>Gets or sets the image's vertical alignment.</summary>
	/// <value>The vertical alignment.</value>
	public string? VerticalAlignment
	{
		get => this.GetValue(ParameterType.Valign);
		set => this.SetDirectValue(ParameterType.Valign, value);
	}
	#endregion

	#region Public Static Methods

	/// <summary>Returns all the links in a gallery node.</summary>
	/// <param name="site">The site the gallery is from.</param>
	/// <param name="factory">The factory to use to create internal links.</param>
	/// <param name="tag">The gallery tag to work on.</param>
	/// <returns>A collection of <see cref="SiteLink"/>s, one for each line in the gallyer tag.</returns>
	public static IEnumerable<SiteLink> FromGalleryNode(Site site, IWikiNodeFactory factory, ITagNode tag)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(factory);
		ArgumentNullException.ThrowIfNull(tag);
		return FromGalleryNode(site, factory, tag);

		static IEnumerable<SiteLink> FromGalleryNode(Site site, IWikiNodeFactory factory, ITagNode tag)
		{
			if (tag.InnerText?.Trim() is string innerText &&
						innerText.Length > 0)
			{
				var ns = site[MediaWikiNamespaces.File];
				var lines = innerText.Split(TextArrays.LineFeed);
				foreach (var line in lines)
				{
					if (line.Trim() is var trimmedLine && trimmedLine.Length > 0)
					{
						var linkNode = factory.LinkNodeFromWikiText("[[" + trimmedLine + " ]]");
						TrimTrailingSpace(linkNode);
						yield return FromLinkNode(ns, linkNode);
					}
				}
			}
		}
	}

	/// <summary>Creates a new SiteLink instance from the provided text.</summary>
	/// <param name="site">The site the link is from.</param>
	/// <param name="link">The text of the link.</param>
	/// <returns>A new SiteLink.</returns>
	/// <remarks>The text may include or exclude surrounding brackets. Pipes in the text are handled properly either way in order to support gallery links.</remarks>
	public static SiteLink FromGalleryText(Site site, string link)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(link);
		link = WikiTextUtilities.DecodeAndNormalize(link);
		var linkNode = CreateLinkNode(link);
		return FromLinkNode(site[MediaWikiNamespaces.File], linkNode);
	}

	/// <summary>Creates a new SiteLink instance from a <see cref="ILinkNode"/>.</summary>
	/// <param name="site">The site the link is from.</param>
	/// <param name="link">The link node.</param>
	/// <returns>A new SiteLink.</returns>
	public static SiteLink FromLinkNode(Site site, ILinkNode link)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(link);
		return FromLinkNode(site[MediaWikiNamespaces.Main], link);
	}

	/// <summary>Creates a new SiteLink instance from a <see cref="ILinkNode"/>.</summary>
	/// <param name="ns">The default namespace. Main for most; File for gallery links.</param>
	/// <param name="link">The link node.</param>
	/// <returns>A new SiteLink.</returns>
	public static SiteLink FromLinkNode(Namespace ns, ILinkNode link)
	{
		ArgumentNullException.ThrowIfNull(ns);
		ArgumentNullException.ThrowIfNull(link);
		var titleText = link.TitleNodes.ToRaw();
		var valueSplit = SplitWhitespace(titleText);
		SiteLink retval = TitleFactory.FromUnvalidated(ns, valueSplit.Value);
		retval.OriginalTitle = titleText;
		retval.TitleWhitespaceBefore = valueSplit.Before;
		retval.TitleWhitespaceAfter = valueSplit.After;
		foreach (var parameter in link.Parameters)
		{
			var valueRaw = parameter.Value.ToRaw();
			retval.InitValue(valueRaw);
		}

		return retval;
	}

	/// <summary>Creates a new SiteLink instance from the provided text.</summary>
	/// <param name="site">The site the link is from.</param>
	/// <param name="link">The text of the link.</param>
	/// <returns>A new SiteLink.</returns>
	/// <remarks>The text may include or exclude surrounding brackets. Pipes in the text are handled properly either way in order to support gallery links.</remarks>
	public static SiteLink FromText(Site site, string link)
	{
		ArgumentNullException.ThrowIfNull(site);
		ArgumentNullException.ThrowIfNull(link);
		var linkNode = CreateLinkNode(link);
		return FromLinkNode(site, linkNode);
	}

	/// <summary>Converts the specified title to a SiteLink and then displays the text of that link.</summary>
	/// <param name="title">The title to display.</param>
	/// <param name="format">The format of the link.</param>
	/// <returns>A string with the wiki-formatted text of the link.</returns>
	public static string ToText(Title title, LinkFormat format = LinkFormat.Plain) => new SiteLink(title).AsLink(format);

	/// <summary>Converts the specified title to a SiteLink and then displays the text of that link.</summary>
	/// <param name="title">The title to display.</param>
	/// <param name="format">The format of the link.</param>
	/// <returns>A string with the wiki-formatted text of the link.</returns>
	public static string ToText(ITitle title, LinkFormat format = LinkFormat.Plain)
	{
		ArgumentNullException.ThrowIfNull(title);
		return new SiteLink(title.Title).AsLink(format);
	}

	/// <summary>Converts the specified title to a SiteLink and then displays the text of that link.</summary>
	/// <param name="title">The title to display.</param>
	/// <param name="format">The format of the link.</param>
	/// <returns>A string with the wiki-formatted text of the link.</returns>
	public static string ToText(IFullTitle title, LinkFormat format = LinkFormat.Plain)
	{
		ArgumentNullException.ThrowIfNull(title);
		return new SiteLink(title).AsLink(format);
	}

	/// <summary>Converts the specified title to a SiteLink and then displays the text of that link.</summary>
	/// <param name="title">The title to display.</param>
	/// <returns>A string with the wiki-formatted text of the link.</returns>
	public static string ToText(ILinkTitle title) => new SiteLink(title).AsLink();
	#endregion

	#region Public Methods

	/// <summary>Returns the current object as link text.</summary>
	/// <param name="format">The default text to use when adding a caption.</param>
	/// <returns>The current title, formatted as a link.</returns>
	public string AsLink(LinkFormat format = LinkFormat.Plain)
	{
		// Manually constructed so we have better control over the construction, as opposed to converting it to a LinkNode and sending it through the raw WikiText formatter.
		// Initialize StringBuilder with a rough starting size so we don't have to expand it much, if at all.
		var sb = new StringBuilder(this.Parameters.Count * 10 + 30);
		sb
			.Append("[[")
			.Append(this.LinkTarget(format == LinkFormat.ForcedLink && this.Title.Namespace.IsForcedLinkSpace));
		foreach (var parameter in this.Parameters)
		{
			sb
				.Append('|')
				.Append(parameter.Value);
		}

		if (this.Text is null)
		{
			if (format == LinkFormat.PipeTrick)
			{
				sb
					.Append('|')
					.Append(this.Title.PipeTrick());
			}
			else if (format == LinkFormat.LabelName)
			{
				sb
					.Append('|')
					.Append(this.Title.LabelName());
			}
		}

		sb.Append("]]");
		return sb.ToString();
	}

	/// <summary>Gets the image size.</summary>
	/// <returns>The image height and width. If either value is missing, a zero will be returned for that value.</returns>
	/// <exception cref="InvalidOperationException">The size text is invalid, and could not be parsed.</exception>
	public (int Height, int Width) GetSize()
	{
		if (this.Dimensions is string dimensions)
		{
			var split = dimensions.Split(SplitX, 2);
			return split.Length switch
			{
				1 => (0, int.TryParse(split[0], NumberStyles.Integer, this.Title.Site.Culture, out var result) ? result : throw NonNumeric),
				2 => (int.TryParse("0" + split[0], NumberStyles.Integer, this.Title.Site.Culture, out var width) ? width : throw NonNumeric, int.TryParse("0" + split[1], NumberStyles.Integer, this.Title.Site.Culture, out var height) ? height : throw NonNumeric),
				_ => (0, 0)
			};
		}

		return (0, 0);
	}

	/// <summary>Returns the full wikitext of the link target without surrounding braces.</summary>
	/// <param name="forcedNsOverride">If <see langword="true"/>, overrides ForcedNamespaceLink to <see langword="true"/>.</param>
	/// <returns>The current link target.</returns>
	public string LinkTarget(bool forcedNsOverride)
	{
		var sb = new StringBuilder()
			.Append(this.TitleWhitespaceBefore)
			.Append(this.ForcedInterwikiLink ? ":" : string.Empty)
			.Append(this.Interwiki == null ? string.Empty : this.Interwiki.Prefix + ':')
			.Append((forcedNsOverride || this.ForcedNamespaceLink) ? ":" : string.Empty)
			.Append(this.Title.Namespace.DecoratedName())
			.Append(this.Title.PageName);
		if (this.Fragment != null)
		{
			sb
				.Append('#')
				.Append(this.Fragment);
		}

		sb.Append(this.TitleWhitespaceAfter);

		return sb.ToString();
	}

	/// <summary>Sets the image size, formatting the <see cref="Dimensions"/> paramter appropriately.</summary>
	/// <param name="height">The height.</param>
	/// <param name="width">The width.</param>
	public void SetSize(int height, int width)
	{
		if (height == 0)
		{
			if (width == 0)
			{
				this.Parameters.Remove(ParameterType.Size);
			}
			else
			{
				this.Dimensions = width.ToStringInvariant();
			}
		}
		else
		{
			this.Dimensions = (width == 0 ? string.Empty : width.ToStringInvariant()) + "x" + height.ToStringInvariant();
		}
	}

	/// <summary>Converts to the link to a <see cref="ILinkNode"/>.</summary>
	/// <returns>A <see cref="ILinkNode"/> containing the parsed link text.</returns>
	public ILinkNode ToLinkNode()
	{
		List<string> values = [];
		foreach (var parameter in this.Parameters)
		{
			var text = parameter.Value.ToString();
			values.Add(text);
		}

		return WikiNodeFactory.DefaultInstance.LinkNodeFromParts(this.LinkTarget(false), values);
	}

	/// <summary>Copies values from the link into a <see cref="ILinkNode"/>.</summary>
	/// <param name="node">The node to update.</param>
	public void UpdateLinkNode(ILinkNode node)
	{
		var thisNode = this.ToLinkNode();
		ArgumentNullException.ThrowIfNull(node);
		node.TitleNodes.Clear();
		node.TitleNodes.AddRange(thisNode.TitleNodes);
		node.Parameters.Clear();
		node.Parameters.AddRange(thisNode.Parameters);
	}

	/// <summary>Creates a new copy of the SiteLink with a different title.</summary>
	/// <param name="title">The title to change to.</param>
	/// <returns>A new copy of the SiteLink with the altered title.</returns>
	public SiteLink WithTitle(Title title)
	{
		SiteLink retval = new(title)
		{
			Coerced = this.Coerced,
			ForcedInterwikiLink = this.ForcedInterwikiLink,
			ForcedNamespaceLink = this.ForcedNamespaceLink,
			Fragment = this.Fragment,
			Interwiki = this.Interwiki,
			OriginalTitle = this.OriginalTitle,
			ParametersDropped = this.ParametersDropped,
			TitleWhitespaceAfter = this.TitleWhitespaceAfter,
			TitleWhitespaceBefore = this.TitleWhitespaceBefore
		};

		foreach (var parameter in this.Parameters)
		{
			retval.Parameters.Add(parameter.Key, parameter.Value);
		}

		return retval;
	}
	#endregion

	#region Public Override Methods

	/// <summary>Returns the full text of the link.</summary>
	/// <returns>A <see cref="string" /> that represents this instance.</returns>
	public override string ToString() => this.AsLink();
	#endregion

	#region Private Static Methods
	private static EmbeddedValue SplitWhitespace(string titleText)
	{
		EmbeddedValue value = new();
		var index = 0;
		while (index < titleText.Length && char.IsWhiteSpace(titleText[index]))
		{
			index++;
		}

		value.Before = string.Empty;
		if (index > 0)
		{
			value.Before = titleText[..index];
			titleText = titleText[index..];
		}

		index = titleText.Length;
		while (index > 0 && char.IsWhiteSpace(titleText[index - 1]))
		{
			index--;
		}

		value.After = string.Empty;
		if (index < titleText.Length)
		{
			value.After = titleText[index..];
			titleText = titleText[..index];
		}

		value.Value = titleText;
		return value;
	}
	#endregion

	#region Private Static Methods
	private static ILinkNode CreateLinkNode(string link)
	{
		// The extra space at the end, and then its later removal, is a kludgey workaround for the rare case of [[Link|Text [http://external]]], which the parser doesn't handle correctly at this point.
		var removeSpace = false;
		if (!link.StartsWith("[[", StringComparison.Ordinal) || !link.EndsWith("]]", StringComparison.Ordinal))
		{
			removeSpace = true;
			link = "[[" + link + " ]]";
		}

		var linkNode = WikiNodeFactory.DefaultInstance.LinkNodeFromWikiText(link);
		if (removeSpace)
		{
			TrimTrailingSpace(linkNode);
		}

		return linkNode;
	}

	private static void InitializeImageInfo(Site site)
	{
		// Initialize internals.
		if (ImageParameterInfo.Count == 0)
		{
			foreach (var word in ImageWords)
			{
				var magic = site.MagicWords[word.Key];
				foreach (var alias in magic.Aliases)
				{
					var split = alias.Split("$1", 2);
					if (split.Length == 1)
					{
						DirectValues.Add(alias, word.Value);
						if ((word.Value == ParameterType.Border || word.Value == ParameterType.Upright) && !PreferredWords.ContainsKey(word.Value))
						{
							PreferredWords.Add(word.Value, alias);
						}
					}
					else
					{
						ImageParameterInfo.Add((word.Value, split[0], split[1]));
					}
				}
			}
		}
	}

	private static void TrimTrailingSpace(ILinkNode linkNode)
	{
		var nodes = linkNode.Parameters.Count == 0
			? linkNode.TitleNodes
			: linkNode.Parameters[^1].Value;
		if (nodes[^1] is ITextNode last)
		{
			if (last.Text.Length == 1)
			{
				nodes.RemoveAt(nodes.Count - 1);
			}
			else
			{
				last.Text = last.Text[0..^1];
			}
		}
	}
	#endregion

	#region Private Methods
	private string? GetValue(ParameterType name) => this.Parameters.TryGetValue(name, out var value) ? value.Value : null;

	private void InitValue(string value)
	{
		ArgumentNullException.ThrowIfNull(value);
		var parameter = SplitWhitespace(value);
		if (!DirectValues.TryGetValue(parameter.Value, out var parameterType))
		{
			parameterType = ParameterType.Caption;
			foreach (var imgParamWord in ImageParameterInfo)
			{
				if (parameter.Value.StartsWith(imgParamWord.Before, StringComparison.Ordinal) && parameter.Value.EndsWith(imgParamWord.After, StringComparison.Ordinal))
				{
					parameterType = imgParamWord.ParameterType;
					parameter.Before += imgParamWord.Before;
					parameter.Value = parameter.Value.Substring(imgParamWord.Before.Length, parameter.Value.Length - imgParamWord.Before.Length - imgParamWord.After.Length);
					parameter.After = imgParamWord.After + parameter.After;
					break;
				}
			}
		}

		if (this.Parameters.ContainsKey(parameterType))
		{
			this.ParametersDropped = true;
			if (parameterType == ParameterType.Caption)
			{
				// Unlike other parameters, the last caption is always used in the event of a conflict, rather than the first, so compensate for that.
				this.Parameters.Remove(parameterType);
				this.Parameters.Add(parameterType, parameter);
			}
		}
		else
		{
			this.Parameters.Add(parameterType, parameter);
		}
	}

	private bool SetDirectValue(ParameterType parameterType, string? value)
	{
		var retval = true;
		if (value == null)
		{
			this.Parameters.Remove(parameterType);
		}
		else if (this.Parameters.TryGetValue(parameterType, out var param))
		{
			param.Value = value;
		}
		else if (parameterType == ParameterType.Caption || (DirectValues.TryGetValue(value, out var valueType) && parameterType == valueType))
		{
			EmbeddedValue paramValue = new(value);
			this.Parameters.Add(parameterType, paramValue);
		}
		else
		{
			retval = false;
		}

		return retval;
	}

	private void SetParameterValue(ParameterType parameterType, string? value)
	{
		if (value == null)
		{
			this.Parameters.Remove(parameterType);
		}
		else if (this.Parameters.TryGetValue(parameterType, out var parameter))
		{
			parameter.Value = value;
		}
		else
		{
			foreach (var info in ImageParameterInfo)
			{
				if (info.ParameterType == parameterType)
				{
					this.Parameters.Add(parameterType, new EmbeddedValue(info.Before, value, info.After));
					break;
				}
			}
		}
	}
	#endregion
}