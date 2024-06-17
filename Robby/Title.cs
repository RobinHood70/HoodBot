#pragma warning disable CA1036 // Override methods on comparable types
// Comparison semantics make no sense outside of sorting, so we strictly implement CompareTo and nothing else.
namespace RobinHood70.Robby
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;

	/// <summary>A structure to hold page Title information.</summary>
	[SuppressMessage("Rules", "MA0097:A class that implements IComparable<T> or IComparable should override comparison operators", Justification = "While sorting makes some sense for a Title, <> comparison operators are likely to be a mistake for PageName comparison.")]
	public sealed class Title : IComparable<Title>, IEquatable<Title>, ITitle
	{
		#region Private Constants
		// The following is taken from DefaultSettings::$wgLegalTitleChars and always assumes the default setting. I believe this is emitted as part of API:Siteinfo, but I wouldn't trust any kind of automated conversion, so better to just leave it as default, which is what 99.99% of wikis will probably use.
		private const string TitleChars = @"[ %!\""$&'()*,\-.\/0-9:;=?@A-Z\\^_`a-z~+\P{IsBasicLatin}-[()（）]]";
		#endregion

		#region Static Fields

		/// <summary>Gets a regular expression matching all comma-like characters in a string.</summary>
		private static readonly Regex LabelCommaRemover =
			new(@"\ *([,，]" + TitleChars + @"*?)\Z", RegexOptions.Compiled | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);

		/// <summary>Gets a regular expression matching all parenthetical text in a string.</summary>
		private static readonly Regex LabelParenthesesRemover =
			new(@"\ *(\(" + TitleChars + @"*?\)|（" + TitleChars + @"*?）)\Z", RegexOptions.Compiled | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Title"/> class.</summary>
		/// <param name="site">The site the page is from.</param>
		/// <param name="ns">The integer namespace of the Title.</param>
		/// <param name="pageName">The page name of the Title.</param>
		public Title([NotNull, ValidatedNotNull] Site site, int ns, [NotNull, ValidatedNotNull] string pageName)
		{
			ArgumentNullException.ThrowIfNull(site);
			ArgumentNullException.ThrowIfNull(pageName);
			this.Namespace = site[ns];
			this.PageName = pageName;
		}

		/// <summary>Initializes a new instance of the <see cref="Title"/> class.</summary>
		/// <param name="ns">The integer namespace of the Title.</param>
		/// <param name="pageName">The page name of the Title.</param>
		public Title([NotNull, ValidatedNotNull] Namespace ns, [NotNull, ValidatedNotNull] string pageName)
		{
			ArgumentNullException.ThrowIfNull(ns);
			ArgumentNullException.ThrowIfNull(pageName);
			this.Namespace = ns;
			this.PageName = pageName;
		}

		private Title()
		{
			throw new InvalidOperationException();
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets a <see cref="Comparison{T}"/> intended for sorting purposes only.</summary>
		/// <remarks>This is defined as a Comparison rather than making the class IComparable since less-than/greater-than semantics don't really make sense outside of sorting.</remarks>
		public static Comparison<Title> SortComparer => new(Compare);
		#endregion

		#region Public Properties

		/// <summary>Gets the namespace object for the Title.</summary>
		/// <value>The namespace.</value>
		public Namespace Namespace { get; }

		/// <summary>Gets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		public string PageName { get; }

		/// <summary>Gets the site to which this Title belongs.</summary>
		/// <value>The site.</value>
		public Site Site => this.Namespace.Site;

		Title ITitle.Title => this;
		#endregion

		#region Operators

		/// <summary>Determines whether one <see cref="Title"/> is equal to another one.</summary>
		/// <param name="left">The first Title to compare.</param>
		/// <param name="right">The second Title to compare.</param>
		/// <returns><see langword="true"/> if the specified Titles are equal; otherwise, <see langword="false"/>.</returns>
		public static bool operator ==(Title left, Title right) => left.Equals(right);

		/// <summary>Determines whether one <see cref="Title"/> is different from another one.</summary>
		/// <param name="left">The first Title to compare.</param>
		/// <param name="right">The second Title to compare.</param>
		/// <returns><see langword="true"/> if the specified Titles are different; otherwise, <see langword="false"/>.</returns>
		public static bool operator !=(Title left, Title right) => !(left == right);
		#endregion

		#region Public Static Methods

		/// <summary>Compares two <see cref="Title"/>s and returns an integer indicating the sort position of the first relative to the second.</summary>
		/// <param name="x">The first Title.</param>
		/// <param name="y">The second Title.</param>
		/// <returns>An integer indicating whether the first Title is less than (-1), equal to (0), or greater than (1) the second Title.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the Site values don't match.</exception>
		/// <remarks>This is not implemented as an IComparer because less-than/greater-than semantics only really apply in the context of sorting. The Comparer is made public primarily for the convenience of other sorting methods.</remarks>
		public static int Compare(Title x, Title y)
		{
			var nsCompare = Namespace.Compare(x.Namespace, y.Namespace);
			return nsCompare != 0
				? nsCompare
				: x.Namespace.ComparePageNames(x.PageName, y.PageName);
		}

		/// <summary>Trims the disambiguator off of a string (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <param name="pageName">The page name to modify.</param>
		/// <returns>The text with the final paranthetical text removed.</returns>
		/// <remarks>No other string processing is done, making this useful when case or embedded invisible characters must be preserved.</remarks>
		public static string ToLabelName(string pageName)
		{
			ArgumentNullException.ThrowIfNull(pageName);
			return LabelParenthesesRemover.Replace(pageName, string.Empty, 1, 1);
		}
		#endregion

		#region Public Methods

		/// <summary>Gets the value corresponding to {{BASEPAGENAME}}.</summary>
		/// <returns>The name of the base page.</returns>
		public string BasePageName() => this.Namespace.AllowsSubpages && this.PageName.LastIndexOf('/') is var subPageLoc && subPageLoc > 0
				? this.PageName[..subPageLoc]
				: this.PageName;

		/// <inheritdoc/>
		public int CompareTo(Title other)
		{
			var nsCompare = Namespace.Compare(this.Namespace, other.Namespace);
			return nsCompare != 0
				? nsCompare
				: this.Namespace.ComparePageNames(this.PageName, other.PageName);
		}

		/// <summary>Determines whether the specified <see cref="Title"/> is equal to the current one.</summary>
		/// <param name="other">The Title to compare with the current one.</param>
		/// <returns><see langword="true"/> if the specified object is equal to the current one; otherwise, <see langword="false"/>.</returns>
		public bool Equals(Title other) => this.Namespace is not null && this.Namespace == other.Namespace && this.Namespace.PageNameEquals(this.PageName, other.PageName);

		/// <summary>Gets the full page name of a Title.</summary>
		/// <returns>The full page name (<c>{{FULLPAGENAME}}</c>) of a Title.</returns>
		public string FullPageName() => this.Namespace is null ? string.Empty : (this.Namespace.DecoratedName() + this.PageName);

		/// <summary>Determines whether this is considered a discussion page on this site.</summary>
		/// <returns><see langword="true"/> if this is either a talk page or other page flagged by the site as a discussion page; otherwise, <see langword="false"/>.</returns>
		public bool IsDiscussionPage() => this.Namespace.IsTalkSpace || this.Site.DiscussionPages.Contains(this);

		/// <summary>Trims the disambiguator off of a title (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <returns>The text with the final paranthetical text removed.</returns>
		public string LabelName() => this.PageName.Length == 0 ? this.PageName : ToLabelName(this.PageName);

		/// <summary>Returns the full wikitext of the link target without surrounding braces.</summary>
		/// <returns>The title, formatted as a link target.</returns>
		public string LinkTarget() => this.Namespace.LinkName() + this.PageName;

		/// <summary>Checks if the provided page name is equal to the title's page name, based on the case-sensitivity for the namespace.</summary>
		/// <param name="other">The title to compare to.</param>
		/// <returns><see langword="true" /> if the two page names are considered the same; otherwise <see langword="false" />.</returns>
		/// <remarks>It is assumed that the namespace for the second page name is equal to the current one, or at least that they have the same case-sensitivy.</remarks>
		public bool PageNameEquals(string other) => this.PageNameEquals(other, true);

		/// <summary>Checks if the provided page name is equal to the title's page name, based on the case-sensitivity for the namespace.</summary>
		/// <param name="other">The page name to compare to.</param>
		/// <param name="normalize">Inidicates whether the page names should be normalized before comparison.</param>
		/// <returns><see langword="true" /> if the two page names are considered the same; otherwise <see langword="false" />.</returns>
		/// <remarks>It is assumed that the namespace for the second page name is equal to the current one, or at least that they have the same case-sensitivy.</remarks>
		public bool PageNameEquals(string other, bool normalize)
		{
			if (normalize)
			{
				other = WikiTextUtilities.DecodeAndNormalize(other).Trim();
			}

			return this.Namespace.PageNameEquals(this.PageName, other, false);
		}

		/// <summary>Gets a name similar to the one that would appear when using the pipe trick on the page (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <returns>The text with the final paranthetical and/or comma-delimited text removed. Note: like the MediaWiki equivalent, when both are present, this will remove text of the form "(text), text", but text of the form ", text (text)" will become ", text".</returns>
		/// <remarks>This doesn't precisely match the pipe trick logic - they differ in their handling of some abnormal page names. For example, with page names of "User:(Test)", ":(Test)", and "(Test)", the pipe trick gives "User:", ":", and "(Test)", respectively. Since this routine ignores the namespace completely and checks for empty return values, it returns "(Test)" consistently in all three cases.</remarks>
		public string PipeTrick()
		{
			var pageName = LabelCommaRemover.Replace(this.PageName, string.Empty, 1, 1);
			return LabelParenthesesRemover.Replace(pageName, string.Empty, 1, 1);
		}

		/// <summary>Gets the value corresponding to {{ROOTPAGENAME}}.</summary>
		/// <returns>The name of the base page.</returns>
		public string RootPageName() =>
			this.Namespace.AllowsSubpages &&
			this.PageName.IndexOf('/', StringComparison.Ordinal) is var subPageLoc &&
			subPageLoc >= 0
				? this.PageName[..subPageLoc]
				: this.PageName;

		/// <summary>Gets a Title object for the Title's corresponding subject page.</summary>
		/// <returns>The subject page.</returns>
		/// <remarks>If the Title is a subject page, returns itself.</remarks>
		public Title SubjectPage() => new(this.Namespace.SubjectSpace, this.PageName);

		/// <summary>Gets the value corresponding to {{SUBPAGENAME}}.</summary>
		/// <returns>The name of the subpage.</returns>
		public string SubPageName() =>
			this.Namespace.AllowsSubpages &&
			(this.PageName.LastIndexOf('/') + 1) is var subPageLoc &&
			subPageLoc > 0
				? this.PageName[subPageLoc..]
				: this.PageName;

		/// <summary>Gets a Title object for the Title's corresponding subject page.</summary>
		/// <returns>The talk page.</returns>
		/// <remarks>If this object represents a talk page, returns a self-reference.</remarks>
		public Title? TalkPage() => this.Namespace.TalkSpace is null
			? null
			: new(this.Namespace.TalkSpace, this.PageName);
		#endregion

		#region Public Override Methods

		/// <summary>Determines whether the specified object is equal to the current <see cref="Title"/>.</summary>
		/// <param name="obj">The object to compare with the current Title.</param>
		/// <returns><see langword="true"/> if the specified object is equal to the current Title; otherwise, <see langword="false"/>.</returns>
		public override bool Equals(object? obj) => obj is Title title && this.Equals(title);

		/// <summary>A hash function combining the hashes of both <see cref="Namespace"/> and <see cref="PageName"/>.</summary>
		/// <returns>A hash code for the current <see cref="Title"/>.</returns>
		public override int GetHashCode() => HashCode.Combine(this.Namespace, this.PageName);

		/// <summary>Returns the full page name of this <see cref="Title"/>.</summary>
		/// <returns>The full page name.</returns>
		public override string ToString() => this.FullPageName();
		#endregion
	}
}
#pragma warning restore CA1036 // Override methods on comparable types