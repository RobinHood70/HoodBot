namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiClasses.Searches;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a wiki link.</summary>
	public class SiteLink : IFullTitle
	{
		#region Fields
		private string interwikiText;
		private InterwikiEntry interwikiObject;
		private string namespaceText;
		private Namespace namespaceObject;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class.</summary>
		/// <param name="site">The Site the link is from.</param>
		public SiteLink([ValidatedNotNull] Site site)
		{
			ThrowNull(site, nameof(site));
			this.Site = site;
		}

		/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class.</summary>
		/// <param name="title">The title to initialize from.</param>
		public SiteLink(ISimpleTitle title)
		{
			ThrowNull(title, nameof(title));
			this.Site = title.Namespace.Site;
			this.namespaceObject = title.Namespace;
			this.PageName = title.PageName;
			this.DisplayText = this.PipeTrick();
			this.Normalize();
		}

		/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class.</summary>
		/// <param name="title">The title to initialize from.</param>
		public SiteLink(IFullTitle title)
		{
			ThrowNull(title, nameof(title));
			this.Site = title.Namespace.Site;
			this.interwikiObject = title.Interwiki;
			this.namespaceObject = title.Namespace;
			this.PageName = title.PageName;
			this.Fragment = title.Fragment;
			this.DisplayText = this.PipeTrick();
			this.Normalize();
		}

		/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class and attempts to parse the text provided.</summary>
		/// <param name="site">The Site the link is from.</param>
		/// <param name="link">The link text to parse.</param>
		public SiteLink(Site site, string link)
			: this(site)
		{
			var parser = this.InitializeFromParser(link);
			this.DisplayParameter = parser.SingleParameter;
		}

		/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class using the specified values.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="pageName">The page name.</param>
		public SiteLink(Namespace ns, string pageName)
			: this(ns, pageName, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class.</summary>
		/// <param name="ns">The namespace the link is in.</param>
		/// <param name="pageName">The name of the page.</param>
		/// <param name="displayText">The display text.</param>
		public SiteLink(Namespace ns, string pageName, string displayText)
			: this(ns?.Site)
		{
			ThrowNull(ns, nameof(ns));
			ThrowNull(pageName, nameof(pageName));
			this.Namespace = ns;
			this.PageName = pageName;
			this.DisplayParameter = displayText == null ? null : new PaddedString(displayText);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the display text (i.e., the value to the right of the pipe). For categories, this is the sortkey.</summary>
		public string DisplayText
		{
			get => this.DisplayParameter?.Value;
			set
			{
				if (value == null)
				{
					this.DisplayParameter = null;
				}
				else
				{
					if (this.DisplayParameter == null)
					{
						this.DisplayParameter = new PaddedString();
					}

					this.DisplayParameter.Value = value;
				}
			}
		}

		/// <summary>Gets or sets the display text parameter (i.e., the value to the right of the pipe). For images, this is the caption; for categories, it's the sortkey.</summary>
		public virtual PaddedString DisplayParameter { get; set; }

		/// <summary>Gets or sets the full name of the page.</summary>
		public string FullPageName
		{
			get => this.BuildFullTitle(new StringBuilder()).ToString();
			set
			{
				var title = new TitleParts(this.Site, value);
				this.InterwikiText = title.OriginalInterwikiText;  // We're using original text here to retain casing, if desired.
				this.NamespaceText = title.OriginalNamespaceText;
				this.PageName = title.OriginalPageNameText;
				this.Fragment = title.Fragment;
			}
		}

		/// <summary>Gets or sets the fragment for the link (i.e., the section/anchor).</summary>
		public string Fragment { get; set; }

		/// <summary>Gets or sets the interwiki data for the link.</summary>
		/// <value>The interwiki data.</value>
		/// <exception cref="InvalidOperationException">When setting the interwiki value, the Site of the value does not match the Site of the link.</exception>
		public InterwikiEntry Interwiki
		{
			get => this.interwikiObject;
			set
			{
				if (value == null)
				{
					this.interwikiObject = null;
					this.interwikiText = null;
				}
				else if (this.Site != value.Site)
				{
					throw new InvalidOperationException(Resources.InvalidSite);
				}
				else
				{
					this.interwikiObject = value;
					if (!string.Equals(this.interwikiText, value.Prefix, StringComparison.OrdinalIgnoreCase))
					{
						this.interwikiText = value.Prefix;
					}
				}
			}
		}

		/// <summary>Gets or sets the interwiki text. It will be validated against the site's interwiki map, and <see cref="Interwiki"/> will be changed if needed.</summary>
		public string InterwikiText
		{
			get => this.interwikiText;
			set
			{
				try
				{
					var iw = value == null ? null : this.Site.InterwikiMap[value];
					this.interwikiObject = iw;
					this.interwikiText = value;
					if (!this.IsLocal)
					{
						this.namespaceObject = null;
					}
				}
				catch (KeyNotFoundException)
				{
					throw;
				}
			}
		}

		/// <summary>Gets a value indicating whether this instance is local, either by having no interwiki value or one that represents the local wiki (e.g., :en:SomeArticle, on English Wikipedia).</summary>
		/// <value><c>true</c> if this instance is local; otherwise, <c>false</c>.</value>
		public bool IsLocal => this.interwikiObject?.LocalWiki ?? true;

		/// <summary>Gets or sets the white space displayed before the link title.</summary>
		/// <value>The leading white space.</value>
		public string NameLeadingWhiteSpace { get; set; }

		/// <summary>Gets or sets the namespace the page is in.</summary>
		/// <value>The namespace.</value>
		/// <exception cref="InvalidOperationException">When setting the namespace value, the Site of the value does not match the Site of the link.</exception>
		public Namespace Namespace
		{
			get => this.namespaceObject;
			set
			{
				if (value != null && this.Site != value.Site)
				{
					throw new InvalidOperationException(Resources.InvalidSite);
				}

				this.namespaceObject = value ?? this.Site.Namespaces[MediaWikiNamespaces.Main];
				if (!this.namespaceObject.Contains(this.namespaceText))
				{
					this.namespaceText = this.namespaceObject.Name;
				}
			}
		}

		/// <summary>Gets or sets the namespace text. It will be validated against the site's namespaces, and <see cref="Namespace"/> will be changed if needed.</summary>
		public string NamespaceText
		{
			get => this.namespaceText;
			set
			{
				try
				{
					if (this.IsLocal)
					{
						value = value ?? string.Empty;
						var ns = this.Site.Namespaces[value];
						this.namespaceObject = ns;
					}

					this.namespaceText = value;
				}
				catch (KeyNotFoundException)
				{
					throw;
				}
			}
		}

		/// <summary>Gets or sets the white space displayed before the link title.</summary>
		/// <value>The leading white space.</value>
		public string NameTrailingWhiteSpace { get; set; }

		/// <summary>Gets or sets the name of the page without the namespace.</summary>
		public string PageName { get; set; }

		/// <summary>Gets the root page of a subpage.</summary>
		/// <remarks>Note that this property returns the pagename up to (but not including) the first slash. This is <em>not necessarily</em> the page directly above, as <c>{{BASEPAGENAME}}</c> would return. For example, <c>Template:Complicated/Subtemplate/Doc</c> would return only <c>Template:Complicated</c>.</remarks>
		public string RootPageName => this.NamespaceText + ':' + this.PageName.Split(TextArrays.Slash, 2)[0];

		/// <summary>Gets the site the link is on.</summary>
		/// <value>The site the link is on.</value>
		public Site Site { get; }
		#endregion

		#region Public Static Methods

		/// <summary>Finds all links within the given text.</summary>
		/// <param name="site">The Site the link is from.</param>
		/// <param name="text">The text to search.</param>
		/// <param name="convertFileLinks">Whether to return purely <see cref="SiteLink"/>s, or include <see cref="ImageLink"/>s where appropriate.</param>
		/// <returns>An enumeration of all links within the text.</returns>
		/// <remarks>No location information is included, so this is most useful when you simply need to scan links rather than alter them.</remarks>
		public static IEnumerable<SiteLink> FindLinks(Site site, string text, bool convertFileLinks)
		{
			var matches = Find().Matches(text);
			foreach (Match match in matches)
			{
				var paramGroup = match.Groups["parameter"];
				var pagename = match.Value.Substring(0, paramGroup.Success ? paramGroup.Captures[0].Index - match.Index : match.Length).Trim(new[] { '[', ']', '|', ' ' });
				var title = new TitleParts(site, pagename);
				if (convertFileLinks && !title.LeadingColon && title.Namespace == MediaWikiNamespaces.File)
				{
					yield return new ImageLink(site, match.Value);
				}
				else
				{
					yield return new SiteLink(site, match.Value);
				}
			}
		}

		// TODO: Update to TryCreate() method after additional parameters (i.e., image link) are handled.

		/// <summary>Determines whether the specified value is a valid link.</summary>
		/// <param name="value">The value to check.</param>
		/// <returns><c>true</c> if the specified value appears to be a link; otherwise, <c>false</c>.</returns>
		/// <remarks>This is a primitive check for surrounding brackets and may report incorrect values in complex situations.</remarks>
		public static bool IsLink(string value) =>
			value != null &&
			value.Length > 4 &&
			value[0] == '[' &&
			value[1] == '[' &&
			value[value.Length - 2] == ']' &&
			value[value.Length - 1] == ']' &&
			value.Substring(2, value.Length - 4).IndexOfAny(TextArrays.SquareBrackets) == -1;

		// TODO: Update to use Interwiki as well.

		/// <summary>Creates a <see cref="Regex"/> to find all links.</summary>
		/// <returns>A <see cref="Regex"/> that finds all links.</returns>
		public static Regex Find() => Find(null, null);

		/// <summary>Creates a <see cref="Regex"/> to find all links matching the values provided.</summary>
		/// <param name="namespaces">The namespaces to search for. Use <c>null</c> to match all namespaces.</param>
		/// <param name="pageNames">The pagenames to search for. Use <c>null</c> to match all pagenames.</param>
		/// <returns>A <see cref="Regex"/> that finds all links matching the values provided. Note that this will match, for example, any of the pagenames given in any of the namespaces given.</returns>
		public static Regex Find(IEnumerable<string> namespaces, IEnumerable<string> pageNames) => Find(null, null, namespaces, pageNames, null);

		/// <summary>Creates a <see cref="Regex"/> to find all links matching the values provided which also have the specified surrounding text.</summary>
		/// <param name="regexBefore">A <see cref="Regex"/> fragment specifying the text to search for before the link. Use <c>null</c> to ignore the text before the link.</param>
		/// <param name="interwikis">The interwiki prefixes to search for. Use <c>null</c> to match all interwiki prefixes.</param>
		/// <param name="namespaces">The namespaces to search for. Use <c>null</c> to match all namespaces.</param>
		/// <param name="pageNames">The pagenames to search for. Use <c>null</c> to match all pagenames.</param>
		/// <param name="regexAfter">A <see cref="Regex"/> fragment specifying the text to search for after the link. Use <c>null</c> to ignore the text after the link.</param>
		/// <returns>A <see cref="Regex"/> that finds all links matching the values provided. Note that this will match, for example, any of the pagenames given in any of the namespaces given.</returns>
		public static Regex Find(string regexBefore, IEnumerable<string> interwikis, IEnumerable<string> namespaces, IEnumerable<string> pageNames, string regexAfter) =>
			FindRaw(
				regexBefore,
				EnumerableRegex(interwikis, SearchCasing.IgnoreCase),
				EnumerableRegex(namespaces, SearchCasing.IgnoreInitialCaps),
				EnumerableRegex(pageNames, SearchCasing.IgnoreInitialCaps),
				regexAfter);

		/// <summary>Creates a <see cref="Regex"/> to find all links matching the values provided which also have the specified surrounding text.</summary>
		/// <param name="regexBefore">A <see cref="Regex"/> fragment specifying the text to search for before the link. Use <c>null</c> to ignore the text before the link.</param>
		/// <param name="regexInterwikis">A <see cref="Regex"/> fragment specifying the interwikis to search for. Use <c>null</c> to match all interwikis.</param>
		/// <param name="regexNamespaces">A <see cref="Regex"/> fragment specifying the namespaces to search for. Use <c>null</c> to match all namespaces.</param>
		/// <param name="regexPageNames">A <see cref="Regex"/> fragment specifying the pagenames to search for. Use <c>null</c> to match all pagenames.</param>
		/// <param name="regexAfter">A <see cref="Regex"/> fragment specifying the text to search for after the link. Use <c>null</c> to ignore the text after the link.</param>
		/// <returns>A <see cref="Regex"/> that finds all links matching the values provided. Note that this will match, for example, any of the pagenames given in any of the namespaces given.</returns>
		public static Regex FindRaw(string regexBefore, string regexInterwikis, string regexNamespaces, string regexPageNames, string regexAfter)
		{
			// TODO: This probably handles nested links reliably, but is a frightful mess and no longer handles searches in parameter text. Tweak internal parser to handle start and end markers, then abort when it finds the matched end marker, whether or not it's the end of the text. After that, searching becomes just a matter of parsing as per normal, and write a Link/Template equivalent to Regex.Replace to go along with that.
			const string regexWildNamespace = @"[^:#\|\]]*?";
			if (regexBefore != null)
			{
				regexBefore = @"(?<before>" + regexBefore + ")";
			}

			var iwOptional = regexInterwikis == null ? "?" : string.Empty;
			var nsOptional = regexNamespaces == null ? "?" : string.Empty;
			regexInterwikis = regexInterwikis ?? regexWildNamespace;
			regexNamespaces = regexNamespaces ?? regexWildNamespace;
			regexPageNames = regexPageNames ?? @"[^#\|\]]*?";
			if (regexAfter != null)
			{
				regexAfter = @"(?<after>" + regexAfter + ")";
			}

			var regexText = regexBefore +
				@"\[\[" +
				"(?<pre>:)?" +
				$"((?<interwiki>{regexInterwikis}):){iwOptional}" +
				$@"\s*((?<namespace>{regexNamespaces}):){nsOptional}" +
				$"(?<pagename>{regexPageNames})" +
				@"(\#(?<fragment>[^\|\]]*?))?" +
				@"(\s*\|\s*(?<parameter>([^\[\]\|]*?(\[\[[^\]]*?]])?)*?))*?" +
				@"\s*]]" +
				regexAfter;

			return new Regex(regexText, RegexOptions.None, TimeSpan.FromSeconds(10));
		}

		/// <summary>Links the text from parts.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="pageName">The name of the page.</param>
		/// <returns>The text of the link to build.</returns>
		public static string LinkTextFromParts(Namespace ns, string pageName) => LinkTextFromParts(ns, pageName, null, null);

		/// <summary>Links the text from parts.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="pageName">The name of the page.</param>
		/// <param name="displayText">The display text. If null, the default "pipe trick" text will be used.</param>
		/// <returns>The text of the link to build.</returns>
		public static string LinkTextFromParts(Namespace ns, string pageName, string displayText) => LinkTextFromParts(ns, pageName, null, displayText);

		/// <summary>Links the text from parts.</summary>
		/// <param name="ns">The namespace.</param>
		/// <param name="pageName">The name of the page.</param>
		/// <param name="fragment">The fragment, if any. May be null.</param>
		/// <param name="displayText">The display text. If null, the default "pipe trick" text will be used.</param>
		/// <returns>The text of the link to build.</returns>
		public static string LinkTextFromParts(Namespace ns, string pageName, string fragment, string displayText)
		{
			var link = new SiteLink(ns, pageName, displayText);
			link.NormalizePageName();
			if (displayText == null)
			{
				link.DisplayText = link.PipeTrick();
			}

			link.NormalizeDisplayText();
			if (fragment != null)
			{
				link.Fragment = fragment;
			}

			return link.ToString();
		}

		/// <summary>Gets the title formatted as a link.</summary>
		/// <param name="title">The title.</param>
		/// <returns>The title formatted as a link.</returns>
		public static string LinkTextFromTitle(ISimpleTitle title)
		{
			ThrowNull(title, nameof(title));
			return new SiteLink(title.Namespace, title.PageName).ToString();
		}

		/// <summary>Gets the title formatted as a link.</summary>
		/// <param name="title">The title.</param>
		/// <returns>The title formatted as a link.</returns>
		public static string LinkTextFromTitle(Title title)
		{
			ThrowNull(title, nameof(title));
			return new SiteLink(title.Namespace, title.PageName, title.LabelName).ToString();
		}
		#endregion

		#region Public Methods

		/// <summary>Builds the link into the specified StringBuilder.</summary>
		/// <param name="builder">The StringBuilder to build into.</param>
		/// <returns>A copy of the <see cref="StringBuilder"/> passed into the method.</returns>
		public StringBuilder Build(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			builder
				.Append("[[")
				.Append(this.NameLeadingWhiteSpace);
			this.BuildFullTitle(builder)
				.Append(this.NameTrailingWhiteSpace);
			return
				this.BuildParameters(builder)
				.Append("]]");
		}

		/// <summary>Indicates whether the current title is equal to another title based on Interwiki, Namespace, PageName, and Fragment.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><see langword="true"/> if the current title is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		public bool FullEquals(IFullTitle other) =>
			other != null &&
			this.Interwiki == other.Interwiki &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName) &&
			this.Fragment == other.Fragment;

		/// <summary>Normalizes all elements of the link.</summary>
		/// <remarks>This method causes the <see cref="InterwikiEntry"/>, <see cref="Namespace"/>, and <see cref="PageName"/> to be reformatted. Interwiki prefixes take on standard casing. Namespace aliases or shortcuts become the full name of the namespace (e.g., <c>Image:</c> becomes <c>File:</c>, <c>WP:</c> becomes <c>Wikipedia:</c>). The <see cref="PageName"/> will also be capitalized according to the rules for the namespace if there's already display text overriding the name. Lastly, the display text will be removed if it matches the value the MediaWiki would display.</remarks>
		public void Normalize()
		{
			this.NormalizeInterwikiText();
			this.NormalizeNamespaceText();
			this.NormalizePageName();
			this.NormalizeDisplayText();
		}

		/// <summary>Normalizes the display text by removing it if it matches the <see cref="FullPageName"/>.</summary>
		public void NormalizeDisplayText()
		{
			if (this.DisplayParameter.Value == this.FullPageName)
			{
				this.DisplayParameter = null;
			}
		}

		/// <summary>Normalizes the interwiki text to its default capitalization.</summary>
		public void NormalizeInterwikiText()
		{
			if (this.Interwiki != null)
			{
				if (this.Interwiki.LocalWiki)
				{
					this.interwikiObject = null;
					this.interwikiText = null;
				}
				else
				{
					this.interwikiText = this.Interwiki?.Prefix;
				}
			}
		}

		/// <summary>Normalizes the namespace text to its primary name.</summary>
		public void NormalizeNamespaceText()
		{
			if (this.Namespace != null)
			{
				this.NamespaceText = this.Namespace.Name;
			}
		}

		/// <summary>Normalizes the name of the page by capitalizing it if the namespace rules allow.</summary>
		/// <remarks>Capitalization will be skipped if the link is an interwiki link or if there is no display text, since changing the name would also change the text.</remarks>
		public void NormalizePageName() => this.NormalizePageName(false);

		/// <summary>Normalizes the name of the page by capitalizing it if the namespace rules allow.</summary>
		/// <param name="ignoreDisplay">If set to true, the page name will be capitalize if possible, even if that would change the resulting text.</param>
		/// <remarks>Capitalization will be skipped if the link is an interwiki link or if there is no display text, since changing the name would also change the text.</remarks>
		public void NormalizePageName(bool ignoreDisplay)
		{
			if (this.Namespace != null && (ignoreDisplay || this.DisplayParameter != null))
			{
				this.PageName = this.Namespace.CapitalizePageName(this.PageName);
			}
		}

		/// <summary>Returns a suggested <see cref="DisplayParameter"/> value based on the link parts, just like the "pipe trick" on a wiki.</summary>
		/// <returns>A suggested <see cref="DisplayParameter"/> value based on the link parts.</returns>
		/// <remarks>This method does not modify the <see cref="DisplayParameter"/> in any way.</remarks>
		public string PipeTrick() => this.PipeTrick(false);

		/// <summary>Returns a suggested <see cref="DisplayParameter"/> value based on the link parts, much like the "pipe trick" on a wiki.</summary>
		/// <param name="useFragmentIfPresent">if set to <c>true</c>, and a fragment exists, uses the fragment to generate the name, rather than the pagename.</param>
		/// <returns>A suggested <see cref="DisplayParameter"/> value based on the link parts.</returns>
		/// <remarks>This method does not modify the <see cref="DisplayParameter"/> in any way.</remarks>
		public string PipeTrick(bool useFragmentIfPresent)
		{
			string retval;
			if (useFragmentIfPresent && !string.IsNullOrWhiteSpace(this.Fragment))
			{
				retval = this.Fragment;
			}
			else
			{
				retval = this.PageName ?? string.Empty;
				var split = retval.Split(TextArrays.Comma, 2);
				if (split.Length == 1)
				{
					var lastIndex = retval.LastIndexOf('(');
					if (retval.LastIndexOf(')') > lastIndex)
					{
						retval = retval.Substring(0, lastIndex);
					}
				}
				else
				{
					retval = split[0];
				}
			}

			return retval.Replace('_', ' ').Trim();
		}

		/// <summary>Indicates whether the current title is equal to another title based on Namespace and PageName only.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><see langword="true"/> if the current title is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false"/>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		public bool SimpleEquals(ISimpleTitle other) =>
			other != null &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName);
		#endregion

		#region Public Virtual Methods

		/// <summary>Reformats the link using the specified formats.</summary>
		/// <param name="nameFormat">Whitespace to add before and after the page name. The <see cref="PaddedString.Value"/> property is ignored.</param>
		/// <param name="valueFormat">Whitespace to add before and after the <see cref="DisplayParameter"/>. The <see cref="PaddedString.Value"/> property is ignored.</param>
		public virtual void Reformat(PaddedString nameFormat, PaddedString valueFormat)
		{
			if (nameFormat != null)
			{
				this.NameLeadingWhiteSpace = nameFormat.LeadingWhiteSpace;
				this.Normalize();
				this.NameTrailingWhiteSpace = nameFormat.TrailingWhiteSpace;
			}

			if (valueFormat != null)
			{
				this.DisplayParameter.LeadingWhiteSpace = valueFormat.LeadingWhiteSpace;
				this.DisplayParameter.TrailingWhiteSpace = valueFormat.TrailingWhiteSpace;
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Converts the <see cref="SiteLink"/> to its full wiki text.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		/// <remarks>This is a simple wrapper around the <see cref="Build(StringBuilder)"/> method.</remarks>
		public override string ToString() => this.Build(new StringBuilder()).ToString();
		#endregion

		#region Protected Methods

		/// <summary>Parses the provided text and sets the name properties.</summary>
		/// <param name="textToParse">The text to parse.</param>
		/// <returns>The parameter parser.</returns>
		protected ParameterParser InitializeFromParser(string textToParse)
		{
			var parser = new ParameterParser(textToParse, true, true, false);
			this.NameLeadingWhiteSpace = parser.Name.LeadingWhiteSpace;
			this.NameTrailingWhiteSpace = parser.Name.TrailingWhiteSpace;
			this.FullPageName = parser.Name;

			return parser;
		}
		#endregion

		#region Protected Virtual Methods

		/// <summary>Builds the parameter text, if needed.</summary>
		/// <param name="builder">The builder to build into.</param>
		/// <returns>A copy of the <see cref="StringBuilder"/> passed to the method.</returns>
		/// <remarks>This is a virtual method, to be overridden by any types that inherit <see cref="SiteLink"/>. The default behaviour is to render only the <see cref="DisplayParameter"/> property if it's non-<see langword="null"/>.</remarks>
		protected virtual StringBuilder BuildParameters(StringBuilder builder)
		{
			ThrowNull(builder, nameof(builder));
			if (this.DisplayParameter != null)
			{
				builder.Append('|');
				this.DisplayParameter.Build(builder);
			}

			return builder;
		}
		#endregion

		#region Private Methods
		private StringBuilder BuildFullTitle(StringBuilder builder)
		{
			if (!string.IsNullOrEmpty(this.InterwikiText))
			{
				builder
					.Append(this.InterwikiText)
					.Append(':');
			}

			if (!string.IsNullOrEmpty(this.NamespaceText))
			{
				builder
					.Append(this.NamespaceText)
					.Append(':');
			}

			builder.Append(this.PageName);
			if (!string.IsNullOrEmpty(this.Fragment))
			{
				builder
					.Append('#')
					.Append(this.Fragment);
			}

			return builder;
		}
		#endregion
	}
}