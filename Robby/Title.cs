﻿namespace RobinHood70.Robby
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
	using RobinHood70.WikiCommon.Parser;

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

	// TODO: Convert this to a Record and rewrite all classes deriving from it (possibly using interfaces with a composite structure, i.e. has a Title rather than is a Title). Consider splitting into a simple NS/Name vs. full Title design, or something similar, so things like SubjectPage/TalkPage can be created without special considerations for Titles within Titles.

	/// <summary>Provides a light-weight holder for titles with several information and manipulation functions.</summary>
	public class Title : IMessageSource, ISimpleTitle
	{
		#region Fields
		private Title? subjectPage;
		private Title? talkPage;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Title"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle"/> to copy values from.</param>
		public Title([NotNull, ValidatedNotNull] ISimpleTitle title)
		{
			title.ThrowNull(nameof(title));
			this.Namespace = title.Namespace.NotNull(nameof(title), nameof(title.Namespace));
			this.PageName = title.PageName.NotNull(nameof(title), nameof(title.PageName));
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
		public Title SubjectPage => this.subjectPage ??= this.Namespace.IsSubjectSpace
			? this
			: TitleFactory.DirectNormalized(this.Namespace.SubjectSpace, this.PageName).ToTitle();

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
		public Title? TalkPage => this.talkPage ??=
			this.Namespace.TalkSpace == null ? null :
			this.Namespace.IsTalkSpace ? this :
			TitleFactory.DirectNormalized(this.Namespace.TalkSpace, this.PageName).ToTitle();

		/// <summary>Gets the site to which this title belongs.</summary>
		/// <value>The site.</value>
		public Site Site => this.Namespace.Site;
		#endregion

		#region Public Static Methods

		/// <summary>Creates a new instance of the <see cref="Title"/> class from the page name, placing it in the default namespace if no other namespace is present in the name.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="defaultNamespace">The default namespace if no namespace is specified in the page name.</param>
		/// <param name="pageName">The page name. If a namespace is present, it will override <paramref name="defaultNamespace"/>.</param>
		/// <returns>A new <see cref="Title"/> with the namespace found in <paramref name="pageName"/>, if there is one, otherwise using <paramref name="defaultNamespace"/>.</returns>
		public static Title Coerce(Site site, int defaultNamespace, string pageName) => TitleFactory.FromName(site.NotNull(nameof(site)), defaultNamespace, pageName.NotNull(nameof(pageName))).ToTitle();

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="node">The <see cref="IBacklinkNode"/> to parse.</param>
		/// <returns>A new FullTitle based on the provided values.</returns>
		public static Title FromBacklinkNode(Site site, IBacklinkNode node)
		{
			var title = node.NotNull(nameof(node)).GetTitleText();
			return TitleFactory.FromName(site.NotNull(nameof(site)), title).ToTitle();
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
			var protection = new ProtectInputItem("create", createProtection.NotNull(nameof(createProtection))) { ExpiryRelative = duration };
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
			reason.ThrowNull(nameof(reason));
			var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				[nameof(reason)] = reason,
			};

			return this.Site.PublishChange(this, parameters, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				var input = new DeleteInput(this.FullPageName) { Reason = reason };
				var retval = this.Site.AbstractionLayer.Delete(input);
				return retval.LogId == 0
					? ChangeStatus.Failure
					: ChangeStatus.Success;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this.SimpleEquals(obj as Title);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.Namespace, this.PageName);

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
		public ChangeValue<IDictionary<string, string>> Move(ISimpleTitle to, string reason, bool suppressRedirect) => this.Move(to.NotNull(nameof(to)).FullPageName, reason, false, false, suppressRedirect);

		/// <summary>Moves the title to the name specified.</summary>
		/// <param name="to">The location to move the title to.</param>
		/// <param name="reason">The reason for the move.</param>
		/// <param name="moveTalk">if set to <see langword="true"/>, moves the talk page as well as the original page.</param>
		/// <param name="moveSubpages">if set to <see langword="true"/>, moves all sub-pages of the original page.</param>
		/// <param name="suppressRedirect">if set to <see langword="true"/>, suppress the redirect that would normally be created.</param>
		/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
		/// <remarks>The original title object will remain unaltered after the move; it will not be updated to reflect the destination.</remarks>
		/// <exception cref="InvalidOperationException">Thrown when either the From page or the To page from the Move result is null.</exception>
		public ChangeValue<IDictionary<string, string>> Move(string to, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect)
		{
			to.ThrowNull(nameof(to));
			reason.ThrowNull(nameof(reason));
			const string subPageName = "/SubPage";
			var disabledResult = new Dictionary<string, string>(StringComparer.Ordinal);
			if (!this.Site.EditingEnabled)
			{
				disabledResult.Add(this.FullPageName, to);
				var talk = this.TalkPage;
				if (moveTalk && talk is not null)
				{
					var toPage = TitleFactory.FromName(this.Site, to).ToTitle();
					if (toPage.TalkPage is Title toTalk)
					{
						disabledResult.Add(talk.FullPageName, toTalk.FullPageName);
					}
				}

				if (moveSubpages && this.Namespace.AllowsSubpages)
				{
					var toSubPage = TitleFactory.FromName(this.Site, to + subPageName).ToTitle();
					disabledResult.Add(this.FullPageName + subPageName, toSubPage.FullPageName);
				}
			}

			var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				[nameof(to)] = to,
				[nameof(reason)] = reason,
				[nameof(moveTalk)] = moveTalk,
				[nameof(moveSubpages)] = moveSubpages,
				[nameof(suppressRedirect)] = suppressRedirect,
			};

			return this.Site.PublishChange(disabledResult, this, parameters, ChangeFunc);

			ChangeValue<IDictionary<string, string>> ChangeFunc()
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
				var dict = new Dictionary<string, string>(StringComparer.Ordinal);
				try
				{
					var retval = this.Site.AbstractionLayer.Move(input);
					foreach (var item in retval)
					{
						if (item.Error != null)
						{
							this.Site.PublishWarning(this, Globals.CurrentCulture(Resources.MovePageWarning, this.FullPageName, to, item.Error.Info));
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
			}
		}

		/// <summary>Moves the title to the name specified.</summary>
		/// <param name="to">The location to move the title to.</param>
		/// <param name="reason">The reason for the move.</param>
		/// <param name="moveTalk">if set to <see langword="true"/>, moves the talk page as well as the original page.</param>
		/// <param name="moveSubpages">if set to <see langword="true"/>, moves all sub-pages of the original page.</param>
		/// <param name="suppressRedirect">if set to <see langword="true"/>, suppress the redirect that would normally be created.</param>
		/// <returns>A value indicating the change status of the move along with the list of pages that were moved and where they were moved to.</returns>
		/// <remarks>The original title object will remain unaltered after the move; it will not be updated to reflect the destination.</remarks>
		public ChangeValue<IDictionary<string, string>> Move(ISimpleTitle to, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect) => this.Move(to.NotNull(nameof(to)).FullPageName, reason, moveTalk, moveSubpages, suppressRedirect);

		/// <summary>Checks if the provided page name is equal to the title's page name, based on the case-sensitivity for the namespace.</summary>
		/// <param name="other">The page name to compare to.</param>
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
			var wikiExpiry = expiry == DateTime.MaxValue ? null : (DateTime?)expiry;
			var protections = new List<ProtectInputItem>(2);
			if (editProtection != null)
			{
				protections.Add(new ProtectInputItem("edit", editProtection) { Expiry = wikiExpiry });
			}

			if (moveProtection != null)
			{
				protections.Add(new ProtectInputItem("move", moveProtection) { Expiry = wikiExpiry });
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
			this.Namespace.PageNameEquals(this.PageName, other.PageName, false);

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
			input.ThrowNull(nameof(input));
			input.Protections.ThrowNull(nameof(input), nameof(input.Protections));
			var inputCount = new List<ProtectInputItem>(input.Protections).Count;
			var result = this.Site.AbstractionLayer.Protect(input);

			// This is a simple count comparison, because matching up inputs and outputs would be more intensive and should not be necessary, barring any breaking changes to the MediaWiki protection algorithm.
			return result.Protections.Count == inputCount;
		}
		#endregion

		#region Private Static Methods

		// A dictionary is probably overkill for three items.
		private static string ProtectionWord(ProtectionLevel level) => level switch
		{
			ProtectionLevel.None => "all",
			ProtectionLevel.Semi => "autoconfirmed",
			ProtectionLevel.Full => "sysop",
			_ => throw new ArgumentOutOfRangeException(nameof(level))
		};
		#endregion

		#region Private Methods
		private ChangeStatus Protect(string reason, ICollection<ProtectInputItem> protections)
		{
			reason.ThrowNull(nameof(reason));
			if (protections.Count == 0)
			{
				return ChangeStatus.NoEffect;
			}

			var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				[nameof(reason)] = reason,
				[nameof(protections)] = protections,
			};

			return this.Site.PublishChange(this, parameters, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				var input = new ProtectInput(this.FullPageName)
				{
					Protections = protections,
					Reason = reason
				};

				return this.Protect(input)
					? ChangeStatus.Success
					: ChangeStatus.Failure;
			}
		}
		#endregion
	}
}
