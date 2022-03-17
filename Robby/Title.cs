namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Properties;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon.Properties;

	#region Public Enumerations

	/// <summary>The possible protection levels on a standard wiki.</summary>
	public enum ProtectionLevel
	{
		/// <summary>Do not make any changes to the protection.</summary>
		NoChange,

		/// <summary>Remove the protection.</summary>
		Remove,

		/// <summary>Change to semi-protection.</summary>
		Semi,

		/// <summary>Change to full-protection.</summary>
		Full,
	}
	#endregion

	/// <summary>Provides a light-weight holder for titles with several information and manipulation functions.</summary>
	public class Title : SimpleTitle, IMessageSource
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Title"/> class.</summary>
		private Title(Namespace ns, string pageName)
			: base(ns, pageName)
		{
		}
		#endregion

		#region Public Static Methods

		/// <summary>Initializes a new instance of the <see cref="FullTitle"/> class.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="node">The <see cref="IBacklinkNode"/> to parse.</param>
		/// <returns>A new FullTitle based on the provided values.</returns>
		public static Title FromBacklinkNode(Site site, IBacklinkNode node)
		{
			var title = node.NotNull(nameof(node)).GetTitleText();
			return FromUnvalidated(site.NotNull(nameof(site)), title);
		}

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="ns">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static Title FromUnvalidated(Namespace ns, string pageName)
		{
			ns.ThrowNull(nameof(ns));
			pageName.ThrowNull(nameof(pageName));
			pageName = ns.CapitalizePageName(WikiTextUtilities.TrimToTitle(pageName));
			return new Title(ns, pageName);
		}

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="nsid">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static Title FromUnvalidated(Site site, int nsid, string pageName) => FromUnvalidated(site.NotNull(nameof(site))[nsid], pageName);

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="fullPageName">Name of the page.</param>
		public static Title FromUnvalidated(Site site, string fullPageName)
		{
			TitleFactory title = TitleFactory.Create(site.NotNull(nameof(site)), MediaWikiNamespaces.Main, WikiTextUtilities.TrimToTitle(fullPageName.NotNull(nameof(fullPageName))));
			return new Title(title.Namespace, title.PageName);
		}

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="ns">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static Title FromValidated(Namespace ns, string pageName) => new(ns.NotNull(nameof(ns)), pageName.NotNull(nameof(pageName)));

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="nsid">The namespace the page is in.</param>
		/// <param name="pageName">Name of the page.</param>
		public static Title FromValidated(Site site, int nsid, string pageName) => FromValidated(site.NotNull(nameof(site))[nsid], pageName);

		/// <summary>Initializes a new instance of the <see cref="TitleFactory"/> class.</summary>
		/// <param name="site">The site.</param>
		/// <param name="fullPageName">Name of the page.</param>
		public static Title FromValidated([NotNull][ValidatedNotNull] Site site, [NotNull][ValidatedNotNull] string fullPageName)
		{
			TitleFactory title = TitleFactory.Create(site.NotNull(nameof(site)), MediaWikiNamespaces.Main, fullPageName.NotNull(nameof(fullPageName)));
			return new(title.Namespace, title.PageName);
		}
		#endregion

		#region Public Methods

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="expiry">The expiry date and time.</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		public ChangeStatus CreateProtect(string reason, ProtectionLevel createProtection, DateTime expiry) =>
			createProtection == ProtectionLevel.NoChange
				? ChangeStatus.NoEffect
				: this.CreateProtect(reason, ProtectionWord(createProtection), expiry);

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="expiry">The expiry date and time.</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		/// <remarks>title version allows custom create-protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="CreateProtect(string, ProtectionLevel, DateTime)"/> version of title call.</remarks>
		public ChangeStatus CreateProtect(string reason, string? createProtection, DateTime expiry)
		{
			if (createProtection == null)
			{
				return ChangeStatus.NoEffect;
			}

			ProtectInputItem protection = new("create", createProtection) { Expiry = expiry };
			return this.Protect(reason, new[] { protection });
		}

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="duration">The duration of the create-protection (e.g., "2 weeks").</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		public ChangeStatus CreateProtect(string reason, ProtectionLevel createProtection, string duration) =>
			createProtection == ProtectionLevel.NoChange
				? ChangeStatus.NoEffect
				: this.CreateProtect(reason, ProtectionWord(createProtection)!, duration);

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="duration">The duration of the create-protection (e.g., "2 weeks").</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		/// <remarks>title version allows custom create-protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="CreateProtect(string, ProtectionLevel, string)"/> version of title call.</remarks>
		public ChangeStatus CreateProtect(string reason, string createProtection, string duration)
		{
			ProtectInputItem protection = new("create", createProtection.NotNull(nameof(createProtection))) { ExpiryRelative = duration };
			return this.Protect(reason, new[] { protection });
		}

		/// <summary>Unprotects the title.</summary>
		/// <param name="reason">The reason for the unprotection.</param>
		/// <returns>A value indicating the change status of the unprotection.</returns>
		public ChangeStatus CreateUnprotect(string reason)
		{
			ProtectInputItem protection = new("create", ProtectionWord(ProtectionLevel.Remove)!);
			return this.Protect(reason, new[] { protection });
		}

		/// <summary>Deletes the title for the specified reason.</summary>
		/// <param name="reason">The reason for the deletion.</param>
		/// <returns>A value indicating the change status of the block.</returns>
		public ChangeStatus Delete(string reason)
		{
			reason.ThrowNull(nameof(reason));
			var site = this.Namespace.Site;
			Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
			{
				[nameof(reason)] = reason,
			};

			return site.PublishChange(site, parameters, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				DeleteInput input = new(this.FullPageName) { Reason = reason };
				var retval = this.Namespace.Site.AbstractionLayer.Delete(input);
				return retval.LogId == 0
					? ChangeStatus.Failure
					: ChangeStatus.Success;
			}
		}

		/// <summary>Returns a value indicating if the page exists. This will trigger a Load operation.</summary>
		/// <returns><see langword="true" /> if the page exists; otherwise <see langword="false" />.</returns>
		public bool Exists() => this.Load(PageModules.None, false).Exists;

		/// <summary>Loads the page found at this title.</summary>
		/// <returns>A page for this title. Can be null if the title is a Special or Media page.</returns>
		public Page Load() => this.Load(PageLoadOptions.Default);

		/// <summary>Loads the page found at this title.</summary>
		/// <param name="modules">The modules to load.</param>
		/// <param name="followRedirects">Indicates whether redirects should be followed when loading.</param>
		/// <returns>A page for this title. Can be null if the title is a Special or Media page.</returns>
		public Page Load(PageModules modules, bool followRedirects) => this.Load(new PageLoadOptions(modules, followRedirects));

		/// <summary>Loads the page found at this title.</summary>
		/// <param name="options">The page load options.</param>
		/// <returns>A page for this title.</returns>
		public Page Load(PageLoadOptions options)
		{
			if (this.Namespace.CanTalk)
			{
				PageCollection? pages = PageCollection.Unlimited(this.Site, options);
				pages.GetTitles(this);
				if (pages.Count == 1)
				{
					return pages[0];
				}
			}

			throw new InvalidOperationException(Globals.CurrentCulture(Resources.PageCouldNotBeLoaded, this.FullPageName));
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
		/// <remarks>title version allows custom protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="Protect(string, ProtectionLevel, ProtectionLevel, DateTime)"/> version of title call.</remarks>
		public ChangeStatus Protect(string reason, string? editProtection, string? moveProtection, DateTime expiry)
		{
			var wikiExpiry = expiry == DateTime.MaxValue ? null : (DateTime?)expiry;
			List<ProtectInputItem> protections = new(2);
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
		/// <remarks>title version allows custom protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="Protect(string, ProtectionLevel, ProtectionLevel, string)"/> version of title call.</remarks>
		public ChangeStatus Protect(string reason, string? editProtection, string? moveProtection, string? duration)
		{
			if (duration == null)
			{
				duration = "infinite";
			}

			List<ProtectInputItem> protections = new(2);
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
		/// <param name="editUnprotect">if set to <see langword="true"/>, removes edit protection.</param>
		/// <param name="moveUnprotect">if set to <see langword="true"/>, removes move protection.</param>
		/// <returns>A value indicating the change status of the unprotection.</returns>
		public ChangeStatus Unprotect(string reason, bool editUnprotect, bool moveUnprotect) => this.Protect(
			reason,
			editUnprotect ? ProtectionLevel.Remove : ProtectionLevel.NoChange,
			moveUnprotect ? ProtectionLevel.Remove : ProtectionLevel.NoChange,
			null);
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override bool Equals(object? obj) => this == (obj as Title);

		/// <inheritdoc/>
		public override int GetHashCode() => HashCode.Combine(this.Namespace, this.PageName);
		#endregion

		#region Private Static Methods

		/// <summary>Protects the title based on the specified input.</summary>
		/// <param name="site">The site the title is on.</param>
		/// <param name="input">The input.</param>
		/// <returns><see langword="true"/> if all protections were set to the specified values.</returns>
		private static bool Protect(Site site, ProtectInput input)
		{
			input.ThrowNull(nameof(input));
			input.Protections.ThrowNull(nameof(input), nameof(input.Protections));
			var inputCount = new List<ProtectInputItem>(input.Protections).Count;
			var result = site.AbstractionLayer.Protect(input);

			// This is a simple count comparison, because matching up inputs and outputs would be more intensive and should not be necessary, barring any breaking changes to the MediaWiki protection algorithm.
			return result.Protections.Count == inputCount;
		}

		// A dictionary is probably overkill for three items.
		private static string? ProtectionWord(ProtectionLevel level) => level switch
		{
			ProtectionLevel.Remove => "all",
			ProtectionLevel.Semi => "autoconfirmed",
			ProtectionLevel.Full => "sysop",
			ProtectionLevel.NoChange => null,
			_ => throw new ArgumentOutOfRangeException(paramName: nameof(level), message: GlobalMessages.InvalidSwitchValue)
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

			Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
			{
				[nameof(reason)] = reason,
				[nameof(protections)] = protections,
			};

			return this.Namespace.Site.PublishChange(this, parameters, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				ProtectInput input = new(this.FullPageName)
				{
					Protections = protections,
					Reason = reason
				};

				return Protect(this.Site, input)
					? ChangeStatus.Success
					: ChangeStatus.Failure;
			}
		}
		#endregion
	}
}
