namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon;
	using static RobinHood70.Robby.Properties.Resources;
	using static RobinHood70.WikiCommon.Globals;

	#region Public Enumerations

	/// <summary>The possible protection levels on a standard wiki.</summary>
	public enum ProtectionLevel
	{
		/// <summary>Do not make any changes to the protection.</summary>
		NoChange,

		/// <summary>Remove the protection.</summary>
		None,

		/// <summary>Change to semi-protection.</summary>
		Semi,

		/// <summary>Change to full-protection.</summary>
		Full,
	}
	#endregion

	/// <summary>Provides a light-weight holder for titles with several information and manipulation functions.</summary>
	public class Title : IKeyedTitle, IMessageSource
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
		/// <param name="fullPageName">The full name of the page.</param>
		public Title(Site site, string fullPageName)
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
			this.Key = this.FullPageName;
		}

		/// <summary>Initializes a new instance of the <see cref="Title" /> class using the namespace and page name.</summary>
		/// <param name="ns">The namespace to which the page belongs.</param>
		/// <param name="pageName">The name of the page without the namespace.</param>
		public Title(Namespace ns, string pageName)
		{
			ThrowNull(ns, nameof(ns));
			ThrowNull(pageName, nameof(pageName));
			pageName = pageName.Normalize();
			this.Namespace = ns;
			this.PageName = ns.CaseSensitive ? pageName : pageName.UpperFirst(ns.Site.Culture);
			this.Key = this.FullPageName;
		}

		/// <summary>Initializes a new instance of the <see cref="Title" /> class, copying the information from another <see cref="ISimpleTitle"/> object.</summary>
		/// <param name="title">The Title object to copy from.</param>
		/// <remarks>Note that the Key property will be set to the new full page name, regardless of the original item's key.</remarks>
		public Title(ISimpleTitle title)
		{
			ThrowNull(title, nameof(title));
			this.Namespace = title.Namespace;
			this.PageName = title.PageName;
			this.Key = this.FullPageName;
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the value corresponding to {{BASEPAGENAME}}.</summary>
		/// <value>The name of the base page.</value>
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
		/// <value>The full name of the page.</value>
		public string FullPageName => this.Namespace.DecoratedName + this.PageName;

		/// <summary>Gets the key to use in dictionary lookups. This is identical to FullPageName (after any decoding and normalization), but unlike FullPagename, cannot be modified after being set.</summary>
		/// <value>The key.</value>
		public string Key { get; }

		/// <summary>Gets a name similar to the one that would appear when using the pipe trick on the page (e.g., "Harry Potter (character)" will produce "Harry Potter").</summary>
		/// <value>The name of the label.</value>
		public string LabelName => PipeTrick(this.PageName);

		/// <summary>Gets or sets the namespace object for the title.</summary>
		/// <value>The namespace.</value>
		public Namespace Namespace { get; protected set; }

		/// <summary>Gets or sets the value corresponding to {{PAGENAME}}.</summary>
		/// <value>The name of the page without the namespace.</value>
		public string PageName { get; protected set; }

		/// <summary>Gets the site the Title is from.</summary>
		/// <value>The site the Title is from.</value>
		public Site Site => this.Namespace.Site;

		/// <summary>Gets a Title object for this Title's corresponding subject page.</summary>
		/// <value>The subject page.</value>
		/// <remarks>If this Title is a subject page, returns itself.</remarks>
		public Title SubjectPage => this.Namespace.IsSubjectSpace ? this : new Title(this.Namespace.SubjectSpace, this.PageName);

		/// <summary>Gets the value corresponding to {{SUBPAGENAME}}.</summary>
		/// <value>The name of the subpage.</value>
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

		/// <summary>Gets a Title object for this Title's corresponding subject page.</summary>
		/// <value>The talk page.</value>
		/// <remarks>If this Title is a talk page, the Title returned will be itself. Returns null for pages which have no associated talk page.</remarks>
		public Title TalkPage =>
			this.Namespace.TalkSpace == null ? null
			: this.Namespace.IsTalkSpace ? this
			: new Title(this.Namespace.TalkSpace, this.PageName);
		#endregion

		#region Public Static Methods

		/// <summary>Identical to the constructor with the same signature, but allows that the page name may or may not have the namespace prepended to it and adjusts accordingly.</summary>
		/// <param name="ns">The namespace to which the page belongs.</param>
		/// <param name="pageName">The name of the page, with or without the corresponding namespace prefix.</param>
		/// <returns>A Title object with the given name in the given namespace.</returns>
		public static Title ForcedNamespace(Namespace ns, string pageName)
		{
			ThrowNull(ns, nameof(ns));
			ThrowNull(pageName, nameof(pageName));
			var titleParts = new TitleParts(ns.Site, pageName);
			if (titleParts.Namespace != ns)
			{
				if (titleParts.Namespace == MediaWikiNamespaces.Main)
				{
					titleParts.Namespace = ns;
				}
				else
				{
					titleParts = new TitleParts(ns.Site, ns.DecoratedName + pageName);
				}
			}

			return new Title(titleParts);
		}

		/// <summary>Builds the full name of the page from the namespace and page name, accounting for Main space.</summary>
		/// <param name="ns">The namespace of the page.</param>
		/// <param name="pageName">The page name.</param>
		/// <param name="fragment">The fragment (section title/anchor) to include.</param>
		/// <returns>The full name of the page from the namespace and page name, accounting for Main space.</returns>
		public static string NameFromParts(Namespace ns, string pageName, string fragment) => ns?.DecoratedName + pageName + (fragment == null ? string.Empty : "#" + fragment);

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

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="expiry">The expiry date and time.</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		public ChangeStatus CreateProtect(string reason, ProtectionLevel createProtection, DateTime expiry) => this.CreateProtect(reason, ProtectionWord(createProtection), expiry);

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="expiry">The expiry date and time.</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		/// <remarks>This version allows custom create-protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="CreateProtect(string, ProtectionLevel, DateTime)"/> version of this call.</remarks>
		public ChangeStatus CreateProtect(string reason, string createProtection, DateTime expiry)
		{
			if (createProtection == null)
			{
				return ChangeStatus.Ignored;
			}

			var protection = new ProtectInputItem("create", createProtection) { Expiry = expiry };
			return this.Protect(reason, new[] { protection });
		}

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="duration">The duration of the create-protection (e.g., "2 weeks").</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		public ChangeStatus CreateProtect(string reason, ProtectionLevel createProtection, string duration) => this.CreateProtect(reason, ProtectionWord(createProtection), duration);

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="duration">The duration of the create-protection (e.g., "2 weeks").</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		/// <remarks>This version allows custom create-protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="CreateProtect(string, ProtectionLevel, string)"/> version of this call.</remarks>
		public ChangeStatus CreateProtect(string reason, string createProtection, string duration)
		{
			if (createProtection == null)
			{
				return ChangeStatus.Ignored;
			}

			var protection = new ProtectInputItem("create", createProtection) { ExpiryRelative = duration };
			return this.Protect(reason, new[] { protection });
		}

		/// <summary>Unprotects the title.</summary>
		/// <param name="reason">The reason for the unprotection.</param>
		/// <returns>A value indicating the change status of the unprotection.</returns>
		public ChangeStatus CreateUnprotect(string reason)
		{
			var protection = new ProtectInputItem("create", ProtectionWord(ProtectionLevel.None));
			return this.Protect(reason, new[] { protection });
		}

		/// <summary>Deletes the title for the specified reason.</summary>
		/// <param name="reason">The reason for the deletion.</param>
		/// <returns>A value indicating the change status of the block.</returns>
		public ChangeStatus Delete(string reason)
		{
			ThrowNull(reason, nameof(reason));
			var retval = this.Site.PublishChange(this, new Dictionary<string, object>
			{
				[nameof(reason)] = reason,
			});
			if (retval == ChangeStatus.Successful)
			{
				var input = new DeleteInput(this.FullPageName)
				{
					Reason = reason
				};

				var result = this.Site.AbstractionLayer.Delete(input);
				if (result.LogId == 0)
				{
					retval |= ChangeStatus.Failed;
				}
			}

			return retval;
		}

		/// <summary>Indicates whether the current title is equal to another title based on Namespace, PageName, and Key.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><c>true</c> if the current title is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "IsSameTitle will return false if this is null, which will then short-circuit the remainder of the comparison.")]
		public bool KeyedEquals(IKeyedTitle other) => this.SimpleEquals(other) && this.Key == other.Key;

		/// <summary>Indicates whether the current title is equal to another title based on Namespace and PageName only.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><c>true</c> if the current title is equal to the <paramref name="other" /> parameter; otherwise, <c>false</c>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		public bool SimpleEquals(ISimpleTitle other) =>
			other != null &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName);

		/// <summary>Moves the title to the name specified.</summary>
		/// <param name="to">The location to move the title to.</param>
		/// <param name="reason">The reason for the move.</param>
		/// <param name="suppressRedirect">if set to <c>true</c>, suppress the redirect that would normally be created.</param>
		/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
		public ChangeValue<IDictionary<string, string>> Move(string to, string reason, bool suppressRedirect) => this.Move(to, reason, false, false, suppressRedirect);

		/// <summary>Moves the title to the name specified.</summary>
		/// <param name="to">The location to move the title to.</param>
		/// <param name="reason">The reason for the move.</param>
		/// <param name="suppressRedirect">if set to <c>true</c>, suppress the redirect that would normally be created.</param>
		/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
		public ChangeValue<IDictionary<string, string>> Move(Title to, string reason, bool suppressRedirect) => this.Move(to?.FullPageName, reason, false, false, suppressRedirect);

		/// <summary>Moves the title to the name specified.</summary>
		/// <param name="to">The location to move the title to.</param>
		/// <param name="reason">The reason for the move.</param>
		/// <param name="moveTalk">if set to <c>true</c>, moves the talk page as well as the original page.</param>
		/// <param name="moveSubpages">if set to <c>true</c>, moves all sub-pages of the original page.</param>
		/// <param name="suppressRedirect">if set to <c>true</c>, suppress the redirect that would normally be created.</param>
		/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
		public ChangeValue<IDictionary<string, string>> Move(string to, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect)
		{
			ThrowNull(to, nameof(to));
			ThrowNull(reason, nameof(reason));
			var dict = new Dictionary<string, string>();
			var status = this.Site.PublishChange(this, new Dictionary<string, object>
			{
				[nameof(to)] = to,
				[nameof(reason)] = reason,
				[nameof(moveTalk)] = moveTalk,
				[nameof(moveSubpages)] = moveSubpages,
				[nameof(suppressRedirect)] = suppressRedirect,
			});

			if (status.HasFlag(ChangeStatus.Ignored))
			{
				dict.Add(this.FullPageName, to);
			}
			else
			{
				if (status != ChangeStatus.Successful)
				{
					var input = new MoveInput(this.FullPageName, to)
					{
						IgnoreWarnings = true,
						MoveSubpages = moveSubpages,
						MoveTalk = moveTalk,
						NoRedirect = suppressRedirect,
						Reason = reason
					};

					MoveResult result;
					try
					{
						result = this.Site.AbstractionLayer.Move(input);
						foreach (var item in result)
						{
							if (item.Error != null)
							{
								this.Site.PublishWarning(this, CurrentCulture(MovePageWarning, this.FullPageName, to, item.Error.Info));
							}
							else
							{
								dict.Add(item.From, item.To);
							}
						}

						var titleParts = new TitleParts(this.Site, to);
						this.Namespace = titleParts.Namespace;
						this.PageName = titleParts.PageName;
					}
					catch (WikiException e)
					{
						this.Site.PublishWarning(this, e.Info);
						status |= ChangeStatus.Failed;
					}
				}
			}

			return new ChangeValue<IDictionary<string, string>>(status, dict);
		}

		/// <summary>Moves the title to the name specified.</summary>
		/// <param name="to">The location to move the title to.</param>
		/// <param name="reason">The reason for the move.</param>
		/// <param name="moveTalk">if set to <c>true</c>, moves the talk page as well as the original page.</param>
		/// <param name="moveSubpages">if set to <c>true</c>, moves all sub-pages of the original page.</param>
		/// <param name="suppressRedirect">if set to <c>true</c>, suppress the redirect that would normally be created.</param>
		/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
		public ChangeValue<IDictionary<string, string>> Move(Title to, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect) => this.Move(to?.FullPageName, reason, moveTalk, moveSubpages, suppressRedirect);

		/// <summary>Protects the title.</summary>
		/// <param name="reason">The reason for the protection.</param>
		/// <param name="editProtection">The edit-protection level.</param>
		/// <param name="moveProtection">The move-protection level.</param>
		/// <param name="expiry">The expiry date and time.</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		public ChangeStatus Protect(string reason, ProtectionLevel editProtection, ProtectionLevel moveProtection, DateTime expiry) => this.Protect(reason, ProtectionWord(editProtection), ProtectionWord(moveProtection), expiry);

		/// <summary>Protects the title.</summary>
		/// <param name="reason">The reason for the protection.</param>
		/// <param name="editProtection">The edit-protection level.</param>
		/// <param name="moveProtection">The move-protection level.</param>
		/// <param name="duration">The duration of the protection (e.g., "2 weeks").</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		public ChangeStatus Protect(string reason, ProtectionLevel editProtection, ProtectionLevel moveProtection, string duration) => this.Protect(reason, ProtectionWord(editProtection), ProtectionWord(moveProtection), duration);

		/// <summary>Protects the title.</summary>
		/// <param name="reason">The reason for the protection.</param>
		/// <param name="editProtection">The edit-protection level.</param>
		/// <param name="moveProtection">The move-protection level.</param>
		/// <param name="expiry">The expiry date and time.</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		/// <remarks>This version allows custom protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="Protect(string, ProtectionLevel, ProtectionLevel, DateTime)"/> version of this call.</remarks>
		public ChangeStatus Protect(string reason, string editProtection, string moveProtection, DateTime expiry)
		{
			var protections = new List<ProtectInputItem>(2);
			if (editProtection != null)
			{
				protections.Add(new ProtectInputItem("edit", editProtection) { Expiry = expiry });
			}

			if (moveProtection != null)
			{
				protections.Add(new ProtectInputItem("move", moveProtection) { Expiry = expiry });
			}

			return this.Protect(reason, protections);
		}

		/// <summary>Protects the title.</summary>
		/// <param name="reason">The reason for the protection.</param>
		/// <param name="editProtection">The edit-protection level.</param>
		/// <param name="moveProtection">The move-protection level.</param>
		/// <param name="duration">The duration of the protection (e.g., "2 weeks").</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		/// <remarks>This version allows custom protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="Protect(string, ProtectionLevel, ProtectionLevel, string)"/> version of this call.</remarks>
		public ChangeStatus Protect(string reason, string editProtection, string moveProtection, string duration)
		{
			if (duration == null)
			{
				duration = "infinite";
			}

			var protections = new List<ProtectInputItem>(2);
			if (editProtection != null)
			{
				protections.Add(new ProtectInputItem("edit", editProtection) { ExpiryRelative = duration });
			}

			if (moveProtection != null)
			{
				protections.Add(new ProtectInputItem("move", moveProtection) { ExpiryRelative = duration });
			}

			return this.Protect(reason, protections);
		}

		/// <summary>Unprotects the title for the specified reason.</summary>
		/// <param name="reason">The reason.</param>
		/// <param name="editUnprotect">if set to <c>true</c>, removes edit protection.</param>
		/// <param name="moveUnprotect">if set to <c>true</c>, removes move protection.</param>
		/// <returns>A value indicating the change status of the unprotection.</returns>
		public ChangeStatus Unprotect(string reason, bool editUnprotect, bool moveUnprotect) => this.Protect(
			reason,
			editUnprotect ? ProtectionLevel.None : ProtectionLevel.NoChange,
			moveUnprotect ? ProtectionLevel.None : ProtectionLevel.NoChange,
			null);
		#endregion

		#region Public Override Methods

		/// <summary>Returns a string that represents the current Title.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString() => this.FullPageName;
		#endregion

		#region Protected Methods

		/// <summary>Protects the title based on the specified input.</summary>
		/// <param name="input">The input.</param>
		/// <returns><c>true</c> if all protections were set to the specified values.</returns>
		protected virtual bool Protect(ProtectInput input)
		{
			ThrowNull(input, nameof(input));
			var inputCount = new List<ProtectInputItem>(input.Protections).Count;
			var result = this.Site.AbstractionLayer.Protect(input);

			// This is a simple count comparison, because matching up inputs and outputs would be more intensive and should not be necessary, barring any breaking changes to the MediaWiki protection algorithm.
			return result.Protections.Count == inputCount;
		}
		#endregion

		#region Private Static Methods

		// A dictionary is probably overkill for three items.
		private static string ProtectionWord(ProtectionLevel level) =>
			level == ProtectionLevel.None ? "all" :
			level == ProtectionLevel.Semi ? "autoconfirmed" :
			level == ProtectionLevel.Full ? "sysop" :
			null;
		#endregion

		#region Private Methods
		private ChangeStatus Protect(string reason, ICollection<ProtectInputItem> protections)
		{
			if (protections.Count == 0)
			{
				return ChangeStatus.Ignored;
			}

			var retval = this.Site.PublishChange(this, new Dictionary<string, object>
			{
				[nameof(reason)] = reason,
				[nameof(protections)] = protections,
			});
			if (retval == ChangeStatus.Successful)
			{
				var input = new ProtectInput(this.FullPageName)
				{
					Protections = protections,
					Reason = reason
				};

				if (!this.Protect(input))
				{
					retval |= ChangeStatus.Failed;
				}
			}

			return retval;
		}
		#endregion
	}
}