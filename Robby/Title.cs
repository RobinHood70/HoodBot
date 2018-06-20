namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using WikiCommon;
	using static Properties.Resources;
	using static WikiCommon.Globals;

	#region Public Enumerations
	public enum ProtectionLevel
	{
		NoChange,
		None,
		Semi,
		Full,
	}
	#endregion

	/// <summary>Provides a light-weight holder for titles and provides several information and manipulation functions.</summary>
	public class Title : IWikiTitle, IEquatable<Title>, IMessageSource
	{
		#region Constants
		// The following is taken from DefaultSettings::$wgLegalTitleChars and always assumes the default setting. I believe this is emitted as part of API:Siteinfo, but I wouldn't trust any kind of automated conversion, so better to just leave it as default, which is what 99.99% of wikis will probably use.
		private const string TitleChars = @"[ %!\""$&'()*,\-.\/0-9:;=?@A-Z\\^_`a-z~+\P{IsBasicLatin}-[()（）]]";
		#endregion

		#region Fields
		private static Regex labelCommaRemover = new Regex(@"\ *([,，]" + TitleChars + @"*?)\Z", RegexOptions.Compiled);
		private static Regex labelParenthesesRemover = new Regex(@"\ *(\(" + TitleChars + @"*?\)|（" + TitleChars + @"*?）)\Z", RegexOptions.Compiled);
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Title" /> class using the site and full page name.</summary>
		/// <param name="site">The site this title is from.</param>
		/// <param name="fullName">The full name of the page.</param>
		public Title(Site site, string fullName)
			: this(site, fullName, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Title" /> class using the site and full page name.</summary>
		/// <param name="site">The site this title is from.</param>
		/// <param name="fullPageName">The page name, including the namespace.</param>
		/// <param name="key">The key to use when indexing this page.</param>
		/// <remarks>Absolutely no cleanup or checking is performed when using this version of the constructor. All values are assumed to already have been validated.</remarks>
		public Title(Site site, string fullPageName, string key)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(fullPageName, nameof(fullPageName));
			var titleParts = new TitleParts(site, fullPageName);
			if (titleParts.Interwiki != null && !titleParts.Interwiki.LocalWiki)
			{
				throw new ArgumentException(CurrentCulture(PageNameInterwiki));
			}

			this.Namespace = titleParts.Namespace;
			this.PageName = titleParts.PageName;
			this.Key = key ?? this.FullPageName;
		}

		/// <summary>Initializes a new instance of the <see cref="Title" /> class using the site and full page name.</summary>
		/// <param name="site">The site this title is from.</param>
		/// <param name="ns">The namespace to which the page belongs.</param>
		/// <param name="pageName">The name (only) of the page.</param>
		/// <remarks>Absolutely no cleanup or checking is performed when using this version of the constructor. All values are assumed to already have been validated.</remarks>
		public Title(Site site, int ns, string pageName)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(pageName, nameof(pageName));
			pageName = pageName.Normalize();
			this.Namespace = site.Namespaces[ns];
			this.PageName = this.Namespace.CaseSensitive ? pageName : pageName.UpperFirst();
			this.Key = this.FullPageName;
		}

		/// <summary>Initializes a new instance of the <see cref="Title" /> class, copying the information from another Title object.</summary>
		/// <param name="title">The Title object to copy from.</param>
		public Title(IWikiTitle title)
		{
			ThrowNull(title, nameof(title));
			this.Namespace = title.Namespace;
			this.PageName = title.PageName;
			this.Key = title.Key;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the value corresponding to {{BASEPAGENAME}}.</summary>
		public string BasePageName
		{
			get
			{
				if (this.Namespace.AllowsSubpages)
				{
					var subpageLoc = this.PageName.LastIndexOf('/');
					if (subpageLoc >= 0)
					{
						return this.PageName.Substring(0, subpageLoc);
					}
				}

				return this.PageName;
			}
		}

		/// <summary>Gets the value corresponding to {{FULLPAGENAME}}.</summary>
		public string FullPageName => this.Namespace.DecoratedName + this.PageName;

		/// <summary>Gets the original, unaltered key to use for dictionaries and the like.</summary>
		public string Key { get; }

		/// <summary>Gets a name similar to the one that would appear when using the pipe trick on the page (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		public string LabelName => PipeTrick(this.PageName);

		/// <summary>Gets or sets the namespace object for the title.</summary>
		public Namespace Namespace { get; protected set; }

		/// <summary>Gets or sets the value corresponding to {{PAGENAME}}.</summary>
		public string PageName { get; protected set; }

		/// <summary>Gets the site the title is intended for.</summary>
		public Site Site => this.Namespace.Site;

		/// <summary>Gets a Title object for this Title's corresponding subject page. If this Title is a subject page, returns itself.</summary>
		public Title SubjectPage => this.Namespace.IsSubjectSpace ? this : new Title(this.Site, this.Namespace.SubjectSpaceId, this.PageName);

		/// <summary>Gets the value corresponding to {{SUBPAGENAME}}.</summary>
		public string SubpageName
		{
			get
			{
				if (this.Namespace.AllowsSubpages)
				{
					var subpageLoc = this.PageName.LastIndexOf('/');
					if (subpageLoc >= 0)
					{
						return this.PageName.Substring(subpageLoc);
					}
				}

				return this.PageName;
			}
		}

		/// <summary>Gets a Title object for this Title's corresponding subject page. If this Title is a talk page, returns itself. Returns null for pages which have no associated talk page.</summary>
		public Title TalkPage =>
			this.Namespace.TalkSpaceId == null ? null
			: this.Namespace.IsTalkSpace ? this
			: new Title(this.Site, this.Namespace.TalkSpaceId.Value, this.PageName);
		#endregion

		#region Public Static Methods

		/// <summary>Identical to the constructor with the same signature, but allows that the page name may or may not have the namespace prepended to it and adjusts accordingly.</summary>
		/// <param name="site">The site this title is from.</param>
		/// <param name="ns">The namespace to which the page belongs.</param>
		/// <param name="pageName">The name of the page, with or without the corresponding namespace prefix.</param>
		/// <returns>A Title object with the given name in the given namespace.</returns>
		public static Title ForcedNamespace(Site site, int ns, string pageName)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(pageName, nameof(pageName));
			return site.NamespaceFromName(pageName) == ns ? new Title(site, pageName) : new Title(site, ns, pageName);
		}

		/// <summary>Builds the full name of the page from the namespace and page name, accounting for Main space.</summary>
		/// <param name="ns">The namespace of the page.</param>
		/// <param name="pageName">The page name.</param>
		/// <param name="fragment">The fragment (section title/anchor) to include.</param>
		/// <returns>The full name of the page from the namespace and page name, accounting for Main space.</returns>
		public static string NameFromParts(Namespace ns, string pageName, string fragment) => ns.DecoratedName + pageName + (fragment == null ? string.Empty : "#" + fragment);

		/// <summary>Gets a name similar to the one that would appear when using the pipe trick on the page (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <param name="pageName">The name of the page, without namespace or fragment text.</param>
		/// <remarks>This doesn't precisely match the pipe trick logic - they differ in their handling of some abnormal page names. For example, with page names of "User:(Test)", ":(Test)", and "(Test)", the pipe trick gives "User:", ":", and "(Test)", respectively. Since this routine ignores the namespace completely and checks for empty return values, it returns "(Test)" consistently in all three cases.</remarks>
		/// <returns>The text with the final paranthetical and/or comma-delimited text removed. Note: like the MediaWiki equivalent, when both are present, this will remove text of the form "(text), text", but text of the form ", text (text)" will become ", text". The text should already have been cleaned using Fixup().</returns>
		public static string PipeTrick(string pageName)
		{
			pageName = labelCommaRemover.Replace(pageName, string.Empty, 1, 1);
			pageName = labelParenthesesRemover.Replace(pageName, string.Empty, 1, 1);
			return pageName;
		}
		#endregion

		#region Public Methods

		public bool CreateProtect(string reason, ProtectionLevel protectionLevel, DateTime expiry)
		{
			if (protectionLevel != ProtectionLevel.NoChange)
			{
				var protection = new ProtectInputItem("create", ProtectionWord(protectionLevel)) { Expiry = expiry };
				return this.Protect(reason, new[] { protection });
			}

			return false;
		}

		public bool CreateUnprotect(string reason)
		{
			var protection = new ProtectInputItem("create", ProtectionWord(ProtectionLevel.None));
			return this.Protect(reason, new[] { protection });
		}

		public bool Delete(string reason)
		{
			ThrowNull(reason, nameof(reason));
			if (!this.Site.AllowEditing)
			{
				this.Site.PublishIgnoredEdit(this, new Dictionary<string, object>
				{
					[nameof(reason)] = reason,
				});
				return true;
			}

			var input = new DeleteInput(this.FullPageName)
			{
				Reason = reason
			};

			var result = this.Site.AbstractionLayer.Delete(input);
			return result.LogId > 0;
		}

		public bool Equals(Title other) =>
			other == null ? false :
			this.Namespace.Equals(other.Namespace) && this.PageName.Equals(other.PageName);

		/// <summary>Checks to see if this Title resolves to the same page as another Title. Key is ignored.</summary>
		/// <param name="title">The title, or derived type, to compare to.</param>
		/// <returns>True if all of Site, Namespace, and PageName are identical between the two Titles.</returns>
		public bool IsSameTitle(IWikiTitle title) =>
			title != null &&
			this.Namespace == title.Namespace &&
			this.PageName == title.PageName;

		/// <summary>Functionally equivalent to Equals(Title), but IEquatable breaks spectacularly on non-sealed types, so is not implemented.</summary>
		/// <param name="title">The title, or derived type, to compare to.</param>
		/// <returns>True if all of Site, Namespace, PageName, and Key are identical between the two Titles.</returns>
		public bool IsSameTitleAndKey(IWikiTitle title) => this.IsSameTitle(title) && this.Key == title?.Key;

		public Dictionary<string, string> Move(string to, string reason, bool suppressRedirect) => this.Move(to, reason, false, false, suppressRedirect);

		public Dictionary<string, string> Move(Title to, string reason, bool suppressRedirect) => this.Move(to?.FullPageName, reason, false, false, suppressRedirect);

		public Dictionary<string, string> Move(string to, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect)
		{
			ThrowNull(to, nameof(to));
			ThrowNull(reason, nameof(reason));
			if (!this.Site.AllowEditing)
			{
				this.Site.PublishIgnoredEdit(this, new Dictionary<string, object>
				{
					[nameof(to)] = to,
					[nameof(reason)] = reason,
					[nameof(moveTalk)] = moveTalk,
					[nameof(moveSubpages)] = moveSubpages,
					[nameof(suppressRedirect)] = suppressRedirect,
				});

				return new Dictionary<string, string> { [this.FullPageName] = to };
			}

			var input = new MoveInput(this.FullPageName, to)
			{
				IgnoreWarnings = true,
				MoveSubpages = moveSubpages,
				MoveTalk = moveTalk,
				NoRedirect = suppressRedirect,
				Reason = reason
			};

			var retval = new Dictionary<string, string>();
			MoveResult result;
			try
			{
				result = this.Site.AbstractionLayer.Move(input);
			}
			catch (WikiException e)
			{
				this.Site.PublishWarning(this, e.Info);
				return null;
			}

			foreach (var item in result)
			{
				if (item.Error != null)
				{
					this.Site.PublishWarning(this, CurrentCulture(MovePageWarning, this.FullPageName, to, item.Error.Info));
				}
				else
				{
					retval.Add(item.From, item.To);
				}
			}

			var titleParts = new TitleParts(this.Site, to);
			this.Namespace = titleParts.Namespace;
			this.PageName = titleParts.PageName;

			return retval;
		}

		public Dictionary<string, string> Move(Title to, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect) => this.Move(to?.FullPageName, reason, moveTalk, moveSubpages, suppressRedirect);

		public bool Protect(string reason, ProtectionLevel createProtection, string relativeExpiry)
		{
			if (createProtection != ProtectionLevel.NoChange)
			{
				var protection = new ProtectInputItem("create", ProtectionWord(createProtection)) { ExpiryRelative = relativeExpiry };
				return this.Protect(reason, new[] { protection });
			}

			return false;
		}

		public bool Protect(string reason, ProtectionLevel editProtection, ProtectionLevel moveProtection, DateTime expiry)
		{
			var protections = new List<ProtectInputItem>(2);
			if (editProtection != ProtectionLevel.NoChange)
			{
				protections.Add(new ProtectInputItem("edit", ProtectionWord(editProtection)) { Expiry = expiry });
			}

			if (moveProtection != ProtectionLevel.NoChange)
			{
				protections.Add(new ProtectInputItem("move", ProtectionWord(moveProtection)) { Expiry = expiry });
			}

			return this.Protect(reason, protections);
		}

		public bool Protect(string reason, ProtectionLevel editProtection, ProtectionLevel moveProtection, string relativeExpiry)
		{
			if (relativeExpiry == null)
			{
				relativeExpiry = "indefinite";
			}

			var protections = new List<ProtectInputItem>(2);
			if (editProtection != ProtectionLevel.NoChange)
			{
				protections.Add(new ProtectInputItem("edit", ProtectionWord(editProtection)) { ExpiryRelative = relativeExpiry });
			}

			if (moveProtection != ProtectionLevel.NoChange)
			{
				protections.Add(new ProtectInputItem("move", ProtectionWord(moveProtection)) { ExpiryRelative = relativeExpiry });
			}

			return this.Protect(reason, protections);
		}

		public bool Unprotect(string reason, bool editUnprotect, bool moveUnprotect)
		{
			var protections = new List<ProtectInputItem>(2);
			if (editUnprotect)
			{
				protections.Add(new ProtectInputItem("edit", ProtectionWord(ProtectionLevel.None)));
			}

			if (moveUnprotect)
			{
				protections.Add(new ProtectInputItem("move", ProtectionWord(ProtectionLevel.None)));
			}

			return this.Protect(reason, protections);
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a string that represents the current Title.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString() => this.FullPageName;
		#endregion

		#region Private Static Methods
		private static string ProtectionWord(ProtectionLevel level)
		{
			switch (level)
			{
				case ProtectionLevel.None:
					return "all";
				case ProtectionLevel.Semi:
					return "autoconfirmed";
				case ProtectionLevel.Full:
					return "sysop";
			}

			return null;
		}
		#endregion

		#region Private Methods
		private bool Protect(string reason, ICollection<ProtectInputItem> protections)
		{
			if (protections.Count == 0)
			{
				return false;
			}

			if (!this.Site.AllowEditing)
			{
				this.Site.PublishIgnoredEdit(this, new Dictionary<string, object>
				{
					[nameof(reason)] = reason,
					[nameof(protections)] = protections,
				});

				return true;
			}

			var input = new ProtectInput(this.FullPageName)
			{
				Protections = protections,
				Reason = reason
			};
			var result = this.Site.AbstractionLayer.Protect(input);

			return result.Protections.Count == protections.Count;
		}
		#endregion
	}
}