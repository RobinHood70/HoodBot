namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiClasses.Searches;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Represents a wiki link.</summary>
	public class SiteLink : TitleParts
	{
		#region Fields
		private string? interwikiText;
		private string namespaceText;
		private string nameLeadingWhitespace = string.Empty;
		private string nameTrailingWhitespace = string.Empty;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class.</summary>
		/// <param name="title">The title to initialize from.</param>
		public SiteLink(ISimpleTitle title)
			: base(title)
		{
			this.namespaceText = this.OriginalNamespaceText;
			this.DisplayText = this.PipeTrick();
			this.Normalize();
		}

		/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class.</summary>
		/// <param name="title">The title to initialize from.</param>
		public SiteLink(IFullTitle title)
			: base(title)
		{
			this.interwikiText = this.OriginalInterwikiText;
			this.namespaceText = this.OriginalNamespaceText;
			this.DisplayText = this.PipeTrick();
			this.Normalize();
		}

		/// <summary>Initializes a new instance of the <see cref="SiteLink"/> class and attempts to parse the text provided.</summary>
		/// <param name="site">The Site the link is from.</param>
		/// <param name="link">The link text to parse.</param>
		public SiteLink(Site site, string link)
			: base(site, link)
		{
			this.Parser = new ParameterParser(link, true, true, false);
			this.interwikiText = this.OriginalInterwikiText; // We're using original text here to retain casing, if desired.
			this.namespaceText = this.OriginalNamespaceText;
			this.NameLeadingWhiteSpace = this.Parser.Name.LeadingWhiteSpace;
			this.NameTrailingWhiteSpace = this.Parser.Name.TrailingWhiteSpace;
			this.DisplayParameter = this.Parser.SingleParameter;
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
		public SiteLink(Namespace ns, string pageName, string? displayText)
			: base((ns ?? throw ArgumentNull(nameof(ns))).Site, ns.Id, pageName)
		{
			this.namespaceText = this.OriginalNamespaceText;
			this.DisplayParameter = displayText == null ? null : new PaddedString(displayText);
		}
		#endregion

		#region Public Properties

		/// <summary>Gets or sets the display text (i.e., the value to the right of the pipe). For categories, this is the sortkey.</summary>
		public string? DisplayText
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
		public virtual PaddedString? DisplayParameter { get; set; }

		/// <summary>Gets or sets the interwiki text. It will be validated against the site's interwiki map, and <see cref="TitleParts.Interwiki"/> will be changed if needed.</summary>
		public string? InterwikiText
		{
			get => this.interwikiText;
			set
			{
				this.interwikiText = value;
				if (value == null)
				{
					this.Interwiki = null;
				}
				else
				{
					try
					{
						this.Interwiki = this.Site.InterwikiMap[value];
					}
					catch (KeyNotFoundException)
					{
						throw;
					}

					if (!this.IsLocal && this.Namespace.Id != 0)
					{
						this.PageName = this.Namespace.DecoratedName + this.PageName;
						this.Namespace = this.Site.Namespaces[MediaWikiNamespaces.Main];
					}
				}
			}
		}

		/// <summary>Gets or sets the white space displayed before the link title.</summary>
		/// <value>The leading white space.</value>
		public string NameLeadingWhiteSpace
		{
			get => this.nameLeadingWhitespace;
			set => this.nameLeadingWhitespace = value ?? string.Empty;
		}

		/// <summary>Gets or sets the white space displayed before the link title.</summary>
		/// <value>The leading white space.</value>
		public string NameTrailingWhiteSpace
		{
			get => this.nameTrailingWhitespace;
			set => this.nameTrailingWhitespace = value ?? string.Empty;
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
						value ??= string.Empty;
						var ns = this.Site.Namespaces[value];
						this.Namespace = ns;
					}

					this.namespaceText = value;
				}
				catch (KeyNotFoundException)
				{
					throw;
				}
			}
		}
		#endregion

		#region Protected Properties

		/// <summary>Gets the parser in use for the Site/link constructor. This allows inheritors to properly parse parameters.</summary>
		protected ParameterParser? Parser { get; } // TODO: This is a positively awful way to do this. Class is likely to be deprecated and eventually completely unused; if it isn't, figure out a better way to do this.
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
			var matches = (IEnumerable<Match>)Find().Matches(text);
			foreach (var match in matches)
			{
				if (match != null)
				{
					var paramGroup = match.Groups["parameter"];
					var pagename = match.Value.Substring(0, paramGroup.Success ? paramGroup.Captures[0].Index - match.Index : match.Length).Trim(new[] { '[', ']', '|', ' ' });
					var title = new TitleParts(site, pagename);
					yield return convertFileLinks && !title.LeadingColon && title.Namespace == MediaWikiNamespaces.File
						? new ImageLink(site, match.Value)
						: new SiteLink(site, match.Value);
				}
			}
		}

		// TODO: Update to TryCreate() method after additional parameters (i.e., image link) are handled.

		/// <summary>Determines whether the specified value is a valid link.</summary>
		/// <param name="value">The value to check.</param>
		/// <returns><see langword="true"/> if the specified value appears to be a link; otherwise, <see langword="false"/>.</returns>
		/// <remarks>This is a primitive check for surrounding brackets and may report incorrect values in complex situations.</remarks>
		public static bool IsLink(string value) =>
			value != null &&
			value.Length > 4 &&
			value[0] == '[' &&
			value[1] == '[' &&
			value[^2] == ']' &&
			value[^1] == ']' &&
			value[2..^2].IndexOfAny(TextArrays.SquareBrackets) == -1;

		// TODO: Update to use Interwiki as well.

		/// <summary>Creates a <see cref="Regex"/> to find all links.</summary>
		/// <returns>A <see cref="Regex"/> that finds all links.</returns>
		public static Regex Find() => Find(null, null);

		/// <summary>Creates a <see cref="Regex"/> to find all links matching the values provided.</summary>
		/// <param name="namespaces">The namespaces to search for. Use <see langword="null"/> to match all namespaces.</param>
		/// <param name="pageNames">The pagenames to search for. Use <see langword="null"/> to match all pagenames.</param>
		/// <returns>A <see cref="Regex"/> that finds all links matching the values provided. Note that this will match, for example, any of the pagenames given in any of the namespaces given.</returns>
		public static Regex Find(IEnumerable<string>? namespaces, IEnumerable<string>? pageNames) => Find(null, null, namespaces, pageNames, null);

		/// <summary>Creates a <see cref="Regex"/> to find all links matching the values provided which also have the specified surrounding text.</summary>
		/// <param name="regexBefore">A <see cref="Regex"/> fragment specifying the text to search for before the link. Use <see langword="null"/> to ignore the text before the link.</param>
		/// <param name="interwikis">The interwiki prefixes to search for. Use <see langword="null"/> to match all interwiki prefixes.</param>
		/// <param name="namespaces">The namespaces to search for. Use <see langword="null"/> to match all namespaces.</param>
		/// <param name="pageNames">The pagenames to search for. Use <see langword="null"/> to match all pagenames.</param>
		/// <param name="regexAfter">A <see cref="Regex"/> fragment specifying the text to search for after the link. Use <see langword="null"/> to ignore the text after the link.</param>
		/// <returns>A <see cref="Regex"/> that finds all links matching the values provided. Note that this will match, for example, any of the pagenames given in any of the namespaces given.</returns>
		public static Regex Find(string? regexBefore, IEnumerable<string>? interwikis, IEnumerable<string>? namespaces, IEnumerable<string>? pageNames, string? regexAfter) =>
			FindRaw(
				regexBefore,
				EnumerableRegex(interwikis, SearchCasing.IgnoreCase),
				EnumerableRegex(namespaces, SearchCasing.IgnoreInitialCaps),
				EnumerableRegex(pageNames, SearchCasing.IgnoreInitialCaps),
				regexAfter);

		/// <summary>Creates a <see cref="Regex"/> to find all links matching the values provided which also have the specified surrounding text.</summary>
		/// <param name="regexBefore">A <see cref="Regex"/> fragment specifying the text to search for before the link. Use <see langword="null"/> to ignore the text before the link.</param>
		/// <param name="regexInterwikis">A <see cref="Regex"/> fragment specifying the interwikis to search for. Use <see langword="null"/> to match all interwikis.</param>
		/// <param name="regexNamespaces">A <see cref="Regex"/> fragment specifying the namespaces to search for. Use <see langword="null"/> to match all namespaces.</param>
		/// <param name="regexPageNames">A <see cref="Regex"/> fragment specifying the pagenames to search for. Use <see langword="null"/> to match all pagenames.</param>
		/// <param name="regexAfter">A <see cref="Regex"/> fragment specifying the text to search for after the link. Use <see langword="null"/> to ignore the text after the link.</param>
		/// <returns>A <see cref="Regex"/> that finds all links matching the values provided. Note that this will match, for example, any of the pagenames given in any of the namespaces given.</returns>
		public static Regex FindRaw(string? regexBefore, string? regexInterwikis, string? regexNamespaces, string? regexPageNames, string? regexAfter)
		{
			// TODO: This probably handles nested links reliably, but is a frightful mess and no longer handles searches in parameter text. Tweak internal parser to handle start and end markers, then abort when it finds the matched end marker, whether or not it's the end of the text. After that, searching becomes just a matter of parsing as per normal, and write a Link/Template equivalent to Regex.Replace to go along with that.
			const string regexWildNamespace = @"[^:#\|\]]*?";
			if (regexBefore != null)
			{
				regexBefore = @"(?<before>" + regexBefore + ")";
			}

			var iwOptional = regexInterwikis == null ? "?" : string.Empty;
			var nsOptional = regexNamespaces == null ? "?" : string.Empty;
			regexInterwikis ??= regexWildNamespace;
			regexNamespaces ??= regexWildNamespace;
			regexPageNames ??= @"[^#\|\]]*?";
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
		public static string LinkTextFromParts(Namespace ns, string pageName, string? fragment, string? displayText)
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

		/// <summary>Normalizes all elements of the link.</summary>
		/// <remarks>This method causes the <see cref="InterwikiEntry"/>, <see cref="Namespace"/>, and <see cref="TitleParts.PageName"/> to be reformatted. Interwiki prefixes take on standard casing. Namespace aliases or shortcuts become the full name of the namespace (e.g., <c>Image:</c> becomes <c>File:</c>, <c>WP:</c> becomes <c>Wikipedia:</c>). The <see cref="TitleParts.PageName"/> will also be capitalized according to the rules for the namespace if there's already display text overriding the name. Lastly, the display text will be removed if it matches the value the MediaWiki would display.</remarks>
		public void Normalize()
		{
			this.NormalizeInterwikiText();
			this.NormalizeNamespaceText();
			this.NormalizePageName();
			this.NormalizeDisplayText();
		}

		/// <summary>Normalizes the display text by removing it if it matches the full title text.</summary>
		public void NormalizeDisplayText()
		{
			var defaultTitle = this.BuildFullTitle(new StringBuilder()).ToString();
			if (this.DisplayParameter?.Value == defaultTitle)
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
					this.Interwiki = null;
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
		/// <param name="useFragmentIfPresent">if set to <see langword="true"/>, and a fragment exists, uses the fragment to generate the name, rather than the pagename.</param>
		/// <returns>A suggested <see cref="DisplayParameter"/> value based on the link parts.</returns>
		/// <remarks>This method does not modify the <see cref="DisplayParameter"/> in any way.</remarks>
		public string PipeTrick(bool useFragmentIfPresent)
		{
			string retval;
			if (useFragmentIfPresent && !string.IsNullOrWhiteSpace(this.Fragment))
			{
				retval = this.Fragment!;
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
				if (this.DisplayParameter == null)
				{
					this.DisplayParameter = new PaddedString();
				}

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
			if (this.LeadingColon)
			{
				builder.Append(':');
			}

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