namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
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
	public class Title : SimpleTitle, IKeyedTitle, IMessageSource, ISimpleTitle
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Title" /> class using the site and full page name.</summary>
		/// <param name="site">The site this title is from.</param>
		/// <param name="fullPageName">The full name of the page.</param>
		public Title([NotNull] Site site, string? fullPageName)
			: this(site, MediaWikiNamespaces.Main, fullPageName ?? string.Empty, false)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="Title" /> class using the namespace and page name.</summary>
		/// <param name="site">The site the title is from.</param>
		/// <param name="defaultNamespace">The default namespace if no namespace is specified in the page name.</param>
		/// <param name="pageName">The page name. If a namespace is present, it will override <paramref name="defaultNamespace"/>.</param>
		/// <param name="forceNamespace">If <see langword="true"/>, the namespace specified will always be used, even if the pageName begins with what looks like a namespace or interwiki prefix.</param>
		public Title(Site site, int defaultNamespace, string pageName, bool forceNamespace)
			: this(new TitleParser(site, defaultNamespace, pageName, forceNamespace)) => this.Coerced = this.Namespace.Id != defaultNamespace;

		/// <summary>Initializes a new instance of the <see cref="Title" /> class, copying the information from another <see cref="ISimpleTitle"/> object.</summary>
		/// <param name="title">The Title object to copy from.</param>
		/// <remarks>Note that the Key property will be set to the new full page name, regardless of the original item's key.</remarks>
		public Title([NotNull] ISimpleTitle title)
			: base((title ?? throw ArgumentNull(nameof(title))).Namespace, title.PageName)
		{
			this.Coerced = false;
			this.Key = title is IKeyedTitle keyed ? keyed.Key : this.FullPageName();
		}
		#endregion

		#region Public Static Properties

		/// <summary>Gets a default equality comparer for Title objects.</summary>
		/// <remarks>This will usually be the desire equality comparer for external usage, where the calling application could change the page name.</remarks>
		public static IEqualityComparer<Title> DefaultEqualityComparer => KeyedTitleEqualityComparer.Instance;

		/// <summary>Gets a default equality comparer for Title objects.</summary>
		/// <remarks>This will usually be the desired equality comparer for internal usage, where the calling application cannot change the page name.</remarks>
		public static IEqualityComparer<Title> SimpleEqualityComparer => SimpleTitleEqualityComparer.Instance;
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a value indicating whether the title was coerced into its namespace, or started there to begin with.</summary>
		/// <value><c>true</c> if coerced into the indicated namespace; otherwise, <c>false</c>.</value>
		public bool Coerced { get; protected set; }

		/// <summary>Gets or sets the key to use in dictionary lookups. This is identical to FullPageName (after any decoding and normalization), but unlike FullPagename, cannot be modified (via PageName) after being set.</summary>
		/// <value>The key.</value>
		public string Key { get; protected set; }

		/// <summary>Gets or sets a value indicating whether the title had a leading colon.</summary>
		/// <value><see langword="true"/> if there was a leading colon; otherwise, <see langword="false"/>.</value>
		public bool LeadingColon { get; set; }
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
					var input = new DeleteInput(this.FullPageName()) { Reason = reason };
					return this.Site.AbstractionLayer.Delete(input).LogId == 0 ? ChangeStatus.Failure : ChangeStatus.Success;
				});
		}

		/// <summary>Gets the article path for the current page.</summary>
		/// <returns>A Uri to the index.php page.</returns>
		public Uri GetArticlePath() => this.Site.GetArticlePath(this.FullPageName());

		/// <summary>Indicates whether the current title is equal to another title based on Namespace, PageName, and Key.</summary>
		/// <param name="other">A title to compare with this one.</param>
		/// <returns><see langword="true"/> if the current title is equal to the <paramref name="other" /> parameter; otherwise, <see langword="false"/>.</returns>
		/// <remarks>This method is named as it is to avoid any ambiguity about what is being checked, as well as to avoid the various issues associated with implementing IEquatable on unsealed types.</remarks>
		[SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "IsSameTitle will return false if this is null, which will then short-circuit the remainder of the comparison.")]
		public bool KeyedEquals(IKeyedTitle other) => this.SimpleEquals(other) && this.Key == other.Key;

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
		public ChangeValue<IDictionary<string, string>> Move(Title to, string reason, bool suppressRedirect)
		{
			ThrowNull(to, nameof(to));
			return this.Move(to.FullPageName(), reason, false, false, suppressRedirect);
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
				fakeResult.Add(this.FullPageName(), to);
				if (moveTalk && this.TalkPage != null)
				{
					var toPage = new Title(this.Site, to);
					if (toPage.TalkPage is Title toTalk)
					{
						fakeResult.Add(this.TalkPage.FullPageName(), toTalk.FullPageName());
					}
				}

				if (moveSubpages && this.Namespace.AllowsSubpages)
				{
					var toSubPage = new Title(this.Site, to + subPageName);
					fakeResult.Add(this.FullPageName() + subPageName, toSubPage.FullPageName());
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
					var input = new MoveInput(this.FullPageName(), to)
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
								this.Site.PublishWarning(this, CurrentCulture(Resources.MovePageWarning, this.FullPageName(), to, item.Error.Info));
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
		public ChangeValue<IDictionary<string, string>> Move(Title to, string reason, bool moveTalk, bool moveSubpages, bool suppressRedirect)
		{
			ThrowNull(to, nameof(to));
			return this.Move(to.FullPageName(), reason, moveTalk, moveSubpages, suppressRedirect);
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

		/// <summary>Returns a <see cref="string" /> that represents this title.</summary>
		/// <param name="forceLink">if set to <c>true</c>, forces link formatting in namespaces that require it (e.g., Category and File), regardless of the value of LeadingColon.</param>
		/// <returns>A <see cref="string" /> that represents this title.</returns>
		public virtual string ToString(bool forceLink)
		{
			var colon = this.LeadingColon || (forceLink && this.Namespace.IsForcedLinkSpace) ? ":" : string.Empty;
			return colon + this.FullPageName();
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
						var input = new ProtectInput(this.FullPageName())
						{
							Protections = protections,
							Reason = reason
						};

						return this.Protect(input) ? ChangeStatus.Success : ChangeStatus.Failure;
					});
		}
	}
	#endregion
}