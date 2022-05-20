namespace RobinHood70.Robby
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	/// <summary>Provides a light-weight holder for titles with several information and manipulation functions.</summary>
	public class Title
	{
		#region Constants
		// The following is taken from DefaultSettings::$wgLegalTitleChars and always assumes the default setting. I believe this is emitted as part of API:Siteinfo, but I wouldn't trust any kind of automated conversion, so better to just leave it as default, which is what 99.99% of wikis will probably use.
		private const string TitleChars = @"[ %!\""$&'()*,\-.\/0-9:;=?@A-Z\\^_`a-z~+\P{IsBasicLatin}-[()（）]]";
		#endregion

		#region Static Fields

		/// <summary>Gets a regular expression matching all comma-like characters in a stirng.</summary>
		private static readonly Regex LabelCommaRemover = new(@"\ *([,，]" + TitleChars + @"*?)\Z", RegexOptions.Compiled | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);

		/// <summary>Gets a regular expression matching all parenthetical text in a stirng.</summary>
		private static readonly Regex LabelParenthesesRemover = new(@"\ *(\(" + TitleChars + @"*?\)|（" + TitleChars + @"*?）)\Z", RegexOptions.Compiled | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
		#endregion

		#region Fields
		private Title? subjectPage;
		private Title? talkPage;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Title"/> class.</summary>
		/// <param name="title">The title to copy from.</param>
		public Title([NotNull, ValidatedNotNull] TitleFactory title)
		{
			title.ThrowNull();
			this.Namespace = title.Namespace;
			this.PageName = title.PageName;
		}

		/// <summary>Initializes a new instance of the <see cref="Title"/> class.</summary>
		/// <param name="title">The title to copy from.</param>
		/// <remarks>Temporary kludge until titles are fully redesigned.</remarks>
		public Title([NotNull, ValidatedNotNull] IFullTitle title)
		{
			title.ThrowNull();
			this.Namespace = title.Namespace;
			this.PageName = title.PageName;
		}

		/// <summary>Initializes a new instance of the <see cref="Title"/> class.</summary>
		/// <param name="title">The title to copy from.</param>
		public Title([NotNull, ValidatedNotNull] Title title)
		{
			title.ThrowNull();
			this.Namespace = title.Namespace;
			this.PageName = title.PageName;
		}

		// This method is strictly internal, since we need a way to create titles from their parts, but this *must* be a validated source.
		internal Title([NotNull, ValidatedNotNull] Namespace ns, [NotNull, ValidatedNotNull] string pageName)
		{
			this.Namespace = ns.NotNull();
			this.PageName = pageName.NotNull();
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the value corresponding to {{BASEPAGENAME}}.</summary>
		/// <returns>The name of the base page.</returns>
		public string BasePageName => this.Namespace.AllowsSubpages && this.PageName.LastIndexOf('/') is var subPageLoc && subPageLoc > 0
				? this.PageName[..subPageLoc]
				: this.PageName;

		/// <summary>Gets the full page name of a title.</summary>
		/// <returns>The full page name (<c>{{FULLPAGENAME}}</c>) of a title.</returns>
		public string FullPageName => this.Namespace.DecoratedName + this.PageName;

		/// <summary>Gets the namespace object for the title.</summary>
		/// <value>The namespace.</value>
		public Namespace Namespace { get; }

		/// <summary>Gets the name of the page without the namespace.</summary>
		/// <value>The name of the page without the namespace.</value>
		public string PageName { get; }

		/// <summary>Gets the value corresponding to {{ROOTPAGENAME}}.</summary>
		/// <returns>The name of the base page.</returns>
		public string RootPageName =>
			this.Namespace.AllowsSubpages &&
			this.PageName.IndexOf('/', StringComparison.Ordinal) is var subPageLoc &&
			subPageLoc >= 0
				? this.PageName[..subPageLoc]
				: this.PageName;

		/// <summary>Gets the site to which this title belongs.</summary>
		/// <value>The site.</value>
		public Site Site => this.Namespace.Site;

		/// <summary>Gets a Title object for title Title's corresponding subject page.</summary>
		/// <returns>The subject page.</returns>
		/// <remarks>If title Title is a subject page, returns itself.</remarks>
		public Title SubjectPage => this.subjectPage ??= TitleFactory.FromValidated(this.Namespace.SubjectSpace, this.PageName);

		/// <summary>Gets the value corresponding to {{SUBPAGENAME}}.</summary>
		/// <returns>The name of the subpage.</returns>
		public string SubPageName =>
			this.Namespace.AllowsSubpages &&
			(this.PageName.LastIndexOf('/') + 1) is var subPageLoc &&
			subPageLoc > 0
				? this.PageName[subPageLoc..]
				: this.PageName;

		/// <summary>Gets a Title object for title Title's corresponding subject page.</summary>
		/// <returns>The talk page.</returns>
		/// <remarks>If this object represents a talk page, returns a self-reference.</remarks>
		public Title? TalkPage => this.Namespace.TalkSpace == null
			? null
			: this.talkPage ??= TitleFactory.FromValidated(this.Namespace.TalkSpace, this.PageName);
		#endregion

		#region Public Methods

		/// <summary>Returns the provided title as link text.</summary>
		/// <returns>The current title, formatted as a link.</returns>
		public string AsLink() => this.AsLink(null);

		/// <summary>Returns the provided title as link text.</summary>
		/// <param name="linkType">The default text to use for the link.</param>
		/// <returns>The current title, formatted as a link.</returns>
		public string AsLink(LinkFormat linkType)
		{
			var text = linkType switch
			{
				LinkFormat.LabelName => this.LabelName(),
				LinkFormat.PipeTrick => this.PipeTrick(),
				LinkFormat.Plain => null,
				_ => throw new ArgumentOutOfRangeException(nameof(linkType)),
			};

			return this.AsLink(text);
		}

		/// <summary>Returns the provided title as link text.</summary>
		/// <param name="linkText">The text to use for the link.</param>
		/// <returns>The current title, formatted as a link.</returns>
		public string AsLink(string? linkText)
		{
			var linkName = this.Namespace.LinkName;
			StringBuilder sb = new(linkName.Length + 5 + (this.PageName.Length << 1));
			sb
				.Append("[[")
				.Append(linkName)
				.Append(this.PageName);
			if (linkText != null)
			{
				sb
					.Append('|')
					.Append(linkText);
			}

			sb.Append("]]");
			return sb.ToString();
		}

		/// <summary>Trims the disambiguator off of a title (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <returns>The text with the final paranthetical text removed.</returns>
		[return: NotNullIfNotNull("title")]
		public string LabelName() => this.PageName.Length == 0 ? this.PageName : LabelParenthesesRemover.Replace(this.PageName, string.Empty, 1, 1);

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
		/// <remarks>This doesn't precisely match the pipe trick logic - they differ in their handling of some abnormal page names. For example, with page names of "User:(Test)", ":(Test)", and "(Test)", the pipe trick gives "User:", ":", and "(Test)", respectively. Since this routine ignores the namespace completely and checks for empty return values, it returns "(Test)" consistently in all three cases.</remarks>
		/// <returns>The text with the final paranthetical and/or comma-delimited text removed. Note: like the MediaWiki equivalent, when both are present, this will remove text of the form "(text), text", but text of the form ", text (text)" will become ", text".</returns>
		public string PipeTrick()
		{
			var pageName = LabelCommaRemover.Replace(this.PageName, string.Empty, 1, 1);
			return LabelParenthesesRemover.Replace(pageName, string.Empty, 1, 1);
		}

		/*
		/// <summary>Compares two <see cref="SimpleTitle"/> objects for namespace and page name equality.</summary>
		/// <param name="other">The object to compare to.</param>
		/// <returns><see langword="true"/> if the Namespace and PageName match, regardless of any other properties.</returns>
		public bool SimpleEquals(this SimpleTitle title, SimpleTitle other) =>
			title == null ? other == null :
			other != null &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName, false);
		*/

		/// <summary>Gets the article path for the current page.</summary>
		/// <returns>A Uri to the index.php page.</returns>
		public Uri GetArticlePath() => this.Site.GetArticlePath(this.FullPageName);

		/// <summary>Compares two objects for <see cref="Namespace"/> and <see cref="PageName"/> equality.</summary>
		/// <param name="other">The object to compare to.</param>
		/// <returns><see langword="true"/> if the Namespace and PageName match, regardless of any other properties.</returns>
		public bool SimpleEquals(Title? other) =>
			other != null &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName, false);
		#endregion

		#region Public Virtual Methods

		/// <summary>Returns a <see cref="string" /> that represents this title.</summary>
		/// <param name="forceLink">if set to <c>true</c>, forces link formatting in namespaces that require it (e.g., Category and File), regardless of the value of LeadingColon.</param>
		/// <returns>A <see cref="string" /> that represents this title.</returns>
		public virtual string ToString(bool forceLink)
		{
			var colon = (forceLink && this.Namespace.IsForcedLinkSpace) ? ":" : string.Empty;
			return colon + this.FullPageName;
		}
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override string ToString() => this.ToString(false);
		#endregion
	}
}
