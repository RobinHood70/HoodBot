namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

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
	public class Title : IMessageSource, IEquatable<Title>, ISimpleTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Title" /> class.</summary>
		/// <param name="ns">The namespace of the title.</param>
		/// <param name="pageName">The page name (without leading namespace).</param>
		public Title([NotNull, ValidatedNotNull] Namespace ns, [NotNull, ValidatedNotNull] string pageName)
		{
			ThrowNull(pageName, nameof(pageName));
			this.Namespace = ns ?? throw ArgumentNull(nameof(ns));
			pageName = WikiTextUtilities.DecodeAndNormalize(pageName).Trim();
			this.PageName = this.Namespace.CapitalizePageName(pageName);
		}

		/// <summary>Initializes a new instance of the <see cref="Title"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle"/> to copy values from.</param>
		public Title([NotNull, ValidatedNotNull] ISimpleTitle title)
		{
			ThrowNull(title, nameof(title));
			this.Namespace = title.Namespace ?? throw PropertyNull(nameof(title), nameof(title.Namespace));
			this.PageName = title.PageName ?? throw PropertyNull(nameof(title), nameof(title.PageName));
		}
		#endregion

		#region Public Properties

		/// <summary>Gets the value corresponding to {{BASEPAGENAME}}.</summary>
		/// <returns>The name of the base page.</returns>
		public string BasePageName => this.Namespace.AllowsSubpages && this.PageName.LastIndexOf('/') is var subPageLoc && subPageLoc > 0
				? this.PageName.Substring(0, subPageLoc)
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
				? this.PageName.Substring(0, subPageLoc)
				: this.PageName;

		/// <summary>Gets a Title object for title Title's corresponding subject page.</summary>
		/// <returns>The subject page.</returns>
		/// <remarks>If title Title is a subject page, returns itself.</remarks>
		public Title SubjectPage =>
			this.Namespace.IsSubjectSpace ? this :
			new Title(this.Namespace.SubjectSpace, this.PageName);

		/// <summary>Gets the value corresponding to {{SUBPAGENAME}}.</summary>
		/// <returns>The name of the subpage.</returns>
		public string SubPageName =>
			this.Namespace.AllowsSubpages &&
			(this.PageName.LastIndexOf('/') + 1) is var subPageLoc &&
			subPageLoc > 0
				? this.PageName.Substring(subPageLoc)
				: this.PageName;

		/// <summary>Gets a Title object for title Title's corresponding subject page.</summary>
		/// <returns>The talk page.</returns>
		/// <remarks>If this object represents a talk page, returns a self-reference.</remarks>
		public Title? TalkPage =>
			this.Namespace.TalkSpace == null ? null :
			this.Namespace.IsTalkSpace ? this :
			new Title(this.Namespace.TalkSpace, this.PageName);

		/// <summary>Gets the site to which this title belongs.</summary>
		/// <value>The site.</value>
		public Site Site => this.Namespace.Site;
		#endregion

		#region Operators

		/// <summary>Implements the operator ==.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><see langword="true"/> if string is equal to any of the names representing the namespace.</returns>
		public static bool operator ==(Title? left, Title? right) => left is null ? right is null : left.Equals(right);

		/// <summary>Implements the operator !=.</summary>
		/// <param name="left">The left-hand side of the comparison.</param>
		/// <param name="right">The right-hand side of the comparison.</param>
		/// <returns><see langword="true"/> if the namespace Site or Id are not equal.</returns>
		public static bool operator !=(Title? left, Title? right) => !(left == right);
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new instance of the <see cref="Title"/> class from the full page name.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="defaultNamespace">The default namespace if no namespace is specified in the page name.</param>
		/// <param name="pageName">The page name. If a namespace is present, it will override <paramref name="defaultNamespace"/>.</param>
		/// <returns>A new <see cref="Title"/> with the namespace found in <paramref name="pageName"/>, if there is one, otherwise using <paramref name="defaultNamespace"/>.</returns>
		public static Title Coerce(Site site, int defaultNamespace, string pageName)
		{
			ThrowNull(site, nameof(site));
			ThrowNull(pageName, nameof(pageName));
			var parser = new TitleParser(site, defaultNamespace, pageName);
			return new Title(parser);
		}

		/// <summary>Creates a new instance of the <see cref="Title"/> class from the namespace ID and page name.</summary>
		/// <param name="site">The site this title is from.</param>
		/// <param name="fullPageName">The full name of the page.</param>
		/// <returns>A new Title based on the provided values.</returns>
		public static Title FromName([NotNull, ValidatedNotNull] Site site, [NotNull, ValidatedNotNull] string fullPageName)
		{
			var parser = new TitleParser(site, MediaWikiNamespaces.Main, fullPageName);
			return new Title(parser);
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
		public ChangeStatus CreateProtect(string reason, string? createProtection, DateTime expiry)
		{
			if (createProtection == null)
			{
				return ChangeStatus.NoEffect;
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
			ThrowNull(createProtection, nameof(createProtection));
			var protection = new ProtectInputItem("create", createProtection) { ExpiryRelative = duration };
			return this.Protect(reason, new[] { protection });
		}

		/// <summary>Unprotects the title.</summary>
		/// <param name="reason">The reason for the unprotection.</param>
		/// <returns>A value indicating the change status of the unprotection.</returns>
		public ChangeStatus CreateUnprotect(string reason)
		{
			var protection = new ProtectInputItem("create", ProtectionWord(ProtectionLevel.None)!);
			return this.Protect(reason, new[] { protection });
		}

		/// <summary>Deletes the title for the specified reason.</summary>
		/// <param name="reason">The reason for the deletion.</param>
		/// <returns>A value indicating the change status of the block.</returns>
		public ChangeStatus Delete(string reason)
		{
			ThrowNull(reason, nameof(reason));
			return this.Site.PublishChange(
				this,
				new Dictionary<string, object?>
				{
					[nameof(reason)] = reason,
				},
				() =>
				{
					var input = new DeleteInput(this.FullPageName) { Reason = reason };
					return this.Site.AbstractionLayer.Delete(input).LogId == 0 ? ChangeStatus.Failure : ChangeStatus.Success;
				});
		}

		/// <inheritdoc/>
		public bool Equals([AllowNull] Title other) => this.SimpleEquals(other);

		/// <inheritdoc/>
		public sealed override bool Equals(object? obj) => this.Equals(obj as Title);

		/// <inheritdoc/>
		public sealed override int GetHashCode() => HashCode.Combine(this.Namespace, this.PageName);

		/// <summary>Gets the article path for the current page.</summary>
		/// <returns>A Uri to the index.php page.</returns>
		public Uri GetArticlePath() => this.Site.GetArticlePath(this.FullPageName);

		/// <summary>Moves the title to the name specified.</summary>
		/// <param name="to">The location to move the title to.</param>
		/// <param name="reason">The reason for the move.</param>
		/// <param name="suppressRedirect">if set to <see langword="true"/>, suppress the redirect that would normally be created.</param>
		/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
		/// <remarks>The original title object will remain unaltered after the move; it will not be updated to reflect the destination.</remarks>
		public ChangeValue<IDictionary<string, string>> Move(string to, string reason, bool suppressRedirect) => this.Move(to, reason, false, false, suppressRedirect);

		/// <summary>Moves the title to the name specified.</summary>
		/// <param name="to">The location to move the title to.</param>
		/// <param name="reason">The reason for the move.</param>
		/// <param name="suppressRedirect">if set to <see langword="true"/>, suppress the redirect that would normally be created.</param>
		/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
		/// <remarks>The original title object will remain unaltered after the move; it will not be updated to reflect the destination.</remarks>
		public ChangeValue<IDictionary<string, string>> Move(ISimpleTitle to, string reason, bool suppressRedirect)
		{
			ThrowNull(to, nameof(to));
			return this.Move(to.FullPageName, reason, false, false, suppressRedirect);
		}

		/// <summary>Moves the title to the name specified.</summary>
		/// <param name="to">The location to move the title to.</param>
		/// <param name="reason">The reason for the move.</param>
		/// <param name="moveTalk">if set to <see langword="true"/>, moves the talk page as well as the original page.</param>
		/// <param name="moveSubpages">if set to <see langword="true"/>, moves all sub-pages of the original page.</param>
		/// <param name="suppressRedirect">if set to <see langword="true"/>, suppress the redirect that would normally be created.</param>
		/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
		/// <remarks>The original title object will remain unaltered after the move; it will not be updated to reflect the destination.</remarks>
		public ChangeValue<IDictionary<string, string>> Move(string to, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect)
		{
			const string subPageName = "/SubPage";

			ThrowNull(to, nameof(to));
			ThrowNull(reason, nameof(reason));
			var fakeResult = new Dictionary<string, string>();
			if (!this.Site.EditingEnabled)
			{
				fakeResult.Add(this.FullPageName, to);
				var talkPage = this.TalkPage;
				if (moveTalk && talkPage != null)
				{
					var toPage = FromName(this.Site, to);
					if (toPage.TalkPage is Title toTalk)
					{
						fakeResult.Add(talkPage.FullPageName, toTalk.FullPageName);
					}
				}

				if (moveSubpages && this.Namespace.AllowsSubpages)
				{
					var toSubPage = FromName(this.Site, to + subPageName);
					fakeResult.Add(this.FullPageName + subPageName, toSubPage.FullPageName);
				}
			}

			return this.Site.PublishChange(
				fakeResult,
				this,
				new Dictionary<string, object?>
				{
					[nameof(to)] = to,
					[nameof(reason)] = reason,
					[nameof(moveTalk)] = moveTalk,
					[nameof(moveSubpages)] = moveSubpages,
					[nameof(suppressRedirect)] = suppressRedirect,
				},
				() =>
				{
					var input = new MoveInput(this.FullPageName, to)
					{
						IgnoreWarnings = true,
						MoveSubpages = moveSubpages,
						MoveTalk = moveTalk,
						NoRedirect = suppressRedirect,
						Reason = reason
					};

					var status = ChangeStatus.Success;
					var dict = new Dictionary<string, string>();
					try
					{
						var result = this.Site.AbstractionLayer.Move(input);
						foreach (var item in result)
						{
							if (item.Error != null)
							{
								this.Site.PublishWarning(this, CurrentCulture(Resources.MovePageWarning, this.FullPageName, to, item.Error.Info));
							}
							else if (item.From != null && item.To != null)
							{
								dict.Add(item.From, item.To);
							}
							else
							{
								throw new InvalidOperationException(); // item.From and/or item.To was null.
							}
						}
					}
					catch (WikiException e)
					{
						this.Site.PublishWarning(this, e.Info ?? e.Message);
						status = ChangeStatus.Failure;
					}

					return new ChangeValue<IDictionary<string, string>>(status, dict);
				});
		}

		/// <summary>Moves the title to the name specified.</summary>
		/// <param name="to">The location to move the title to.</param>
		/// <param name="reason">The reason for the move.</param>
		/// <param name="moveTalk">if set to <see langword="true"/>, moves the talk page as well as the original page.</param>
		/// <param name="moveSubpages">if set to <see langword="true"/>, moves all sub-pages of the original page.</param>
		/// <param name="suppressRedirect">if set to <see langword="true"/>, suppress the redirect that would normally be created.</param>
		/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
		/// <remarks>The original title object will remain unaltered after the move; it will not be updated to reflect the destination.</remarks>
		public ChangeValue<IDictionary<string, string>> Move(ISimpleTitle to, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect)
		{
			ThrowNull(to, nameof(to));
			return this.Move(to.FullPageName, reason, moveTalk, moveSubpages, suppressRedirect);
		}

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
		public ChangeStatus Protect(string reason, ProtectionLevel editProtection, ProtectionLevel moveProtection, string? duration) => this.Protect(reason, ProtectionWord(editProtection), ProtectionWord(moveProtection), duration);

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
		public ChangeStatus Protect(string reason, string? editProtection, string? moveProtection, string? duration)
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

		/// <summary>Compares two objects for <see cref="Namespace"/> and <see cref="PageName"/> equality.</summary>
		/// <param name="other">The object to compare to.</param>
		/// <returns><see langword="true"/> if the Namespace and PageName match, regardless of any other properties.</returns>
		public bool SimpleEquals(ISimpleTitle? other) =>
			other != null &&
			this.Namespace == other.Namespace &&
			this.Namespace.PageNameEquals(this.PageName, other.PageName);

		/// <summary>Returns a <see cref="string" /> that represents this title.</summary>
		/// <param name="forceLink">if set to <c>true</c>, forces link formatting in namespaces that require it (e.g., Category and File), regardless of the value of LeadingColon.</param>
		/// <returns>A <see cref="string" /> that represents this title.</returns>
		public virtual string ToString(bool forceLink)
		{
			var colon = (forceLink && this.Namespace.IsForcedLinkSpace) ? ":" : string.Empty;
			return colon + this.FullPageName;
		}

		/// <summary>Unprotects the title for the specified reason.</summary>
		/// <param name="reason">The reason.</param>
		/// <param name="editUnprotect">if set to <see langword="true"/>, removes edit protection.</param>
		/// <param name="moveUnprotect">if set to <see langword="true"/>, removes move protection.</param>
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
		public override string ToString() => this.ToString(false);
		#endregion

		#region Protected Methods

		/// <summary>Protects the title based on the specified input.</summary>
		/// <param name="input">The input.</param>
		/// <returns><see langword="true"/> if all protections were set to the specified values.</returns>
		protected virtual bool Protect(ProtectInput input)
		{
			ThrowNull(input, nameof(input));
			ThrowNull(input.Protections, nameof(input), nameof(input.Protections));
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
			throw new ArgumentOutOfRangeException(nameof(level));
		#endregion

		#region Private Methods
		private ChangeStatus Protect(string reason, ICollection<ProtectInputItem> protections)
		{
			ThrowNull(reason, nameof(reason));
			return protections.Count == 0 ? ChangeStatus.NoEffect :
				this.Site.PublishChange(
					this,
					new Dictionary<string, object?>
					{
						[nameof(reason)] = reason,
						[nameof(protections)] = protections,
					},
					() =>
					{
						var input = new ProtectInput(this.FullPageName)
						{
							Protections = protections,
							Reason = reason
						};

						return this.Protect(input) ? ChangeStatus.Success : ChangeStatus.Failure;
					});
		}
		#endregion
	}
}