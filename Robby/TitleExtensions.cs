namespace RobinHood70.Robby
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;

	/// <summary>These extensions add a number of useful pseudo-properties and methods that can all be determined from the base namespace and page name properties of any <see cref="ISimpleTitle"/>.</summary>
	public static class TitleExtensions
	{
		#region Public Property=Like Methods

		/// <summary>Gets the value corresponding to {{BASEPAGENAME}}.</summary>
		/// <returns>The name of the base page.</returns>
		public static string BasePageName(this ISimpleTitle title) => title.NotNull(nameof(title)).Namespace.AllowsSubpages && title.PageName.LastIndexOf('/') is var subPageLoc && subPageLoc > 0
				? title.PageName.Substring(0, subPageLoc)
				: title.PageName;

		/// <summary>Gets the full page name of a title.</summary>
		/// <returns>The full page name (<c>{{FULLPAGENAME}}</c>) of a title.</returns>
		public static string FullPageName(this ISimpleTitle title) => title.NotNull(nameof(title)).Namespace.DecoratedName + title.PageName;

		/// <summary>Gets the value corresponding to {{ROOTPAGENAME}}.</summary>
		/// <returns>The name of the base page.</returns>
		public static string RootPageName(this ISimpleTitle title) =>
			title.NotNull(nameof(title)).Namespace.AllowsSubpages &&
			title.PageName.IndexOf('/', StringComparison.Ordinal) is var subPageLoc &&
			subPageLoc >= 0
				? title.PageName.Substring(0, subPageLoc)
				: title.PageName;

		/// <summary>Gets a Title object for title Title's corresponding subject page.</summary>
		/// <returns>The subject page.</returns>
		/// <remarks>If title Title is a subject page, returns itself.</remarks>
		public static ISimpleTitle SubjectPage(this ISimpleTitle title) => title.NotNull(nameof(title)).Namespace.IsSubjectSpace
			? title
			: TitleFactory.DirectNormalized(title.Namespace.SubjectSpace, title.PageName).ToTitle();

		/// <summary>Gets the value corresponding to {{SUBPAGENAME}}.</summary>
		/// <returns>The name of the subpage.</returns>
		public static string SubPageName(this ISimpleTitle title) =>
			title.NotNull(nameof(title)).Namespace.AllowsSubpages &&
			(title.PageName.LastIndexOf('/') + 1) is var subPageLoc &&
			subPageLoc > 0
				? title.PageName[subPageLoc..]
				: title.PageName;

		/// <summary>Gets a Title object for title Title's corresponding subject page.</summary>
		/// <returns>The talk page.</returns>
		/// <remarks>If title object represents a talk page, returns a self-reference.</remarks>
		public static ISimpleTitle? TalkPage(this ISimpleTitle title)
		{
			title.ThrowNull(nameof(title));
			return
				title.Namespace.TalkSpace == null ? null :
				title.Namespace.IsTalkSpace ? title :
				TitleFactory.DirectNormalized(title.Namespace.TalkSpace, title.PageName).ToTitle();
		}
		#endregion

		#region Public Methods

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="title">The title to protect from creation.</param>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="expiry">The expiry date and time.</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		public static ChangeStatus CreateProtect(this ISimpleTitle title, string reason, ProtectionLevel createProtection, DateTime expiry) => title.CreateProtect(reason, ProtectionWord(createProtection), expiry);

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="title">The title to protect from creation.</param>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="expiry">The expiry date and time.</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		/// <remarks>title version allows custom create-protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="CreateProtect(ISimpleTitle, string, ProtectionLevel, DateTime)"/> version of title call.</remarks>
		public static ChangeStatus CreateProtect(this ISimpleTitle title, string reason, string? createProtection, DateTime expiry)
		{
			if (createProtection == null)
			{
				return ChangeStatus.NoEffect;
			}

			var protection = new ProtectInputItem("create", createProtection) { Expiry = expiry };
			return title.NotNull(nameof(title)).Protect(reason, new[] { protection });
		}

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="title">The title to protect from creation.</param>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="duration">The duration of the create-protection (e.g., "2 weeks").</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		public static ChangeStatus CreateProtect(this ISimpleTitle title, string reason, ProtectionLevel createProtection, string duration) => title.CreateProtect(reason, ProtectionWord(createProtection), duration);

		/// <summary>Protects a non-existent page from being created.</summary>
		/// <param name="title">The title to protect from creation.</param>
		/// <param name="reason">The reason for the create-protection.</param>
		/// <param name="createProtection">The protection level.</param>
		/// <param name="duration">The duration of the create-protection (e.g., "2 weeks").</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		/// <remarks>title version allows custom create-protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="CreateProtect(ISimpleTitle, string, ProtectionLevel, string)"/> version of title call.</remarks>
		public static ChangeStatus CreateProtect(this ISimpleTitle title, string reason, string createProtection, string duration)
		{
			var protection = new ProtectInputItem("create", createProtection.NotNull(nameof(createProtection))) { ExpiryRelative = duration };
			return title.NotNull(nameof(title)).Protect(reason, new[] { protection });
		}

		/// <summary>Unprotects the title.</summary>
		/// <param name="title">The title to remove creation protection from.</param>
		/// <param name="reason">The reason for the unprotection.</param>
		/// <returns>A value indicating the change status of the unprotection.</returns>
		public static ChangeStatus CreateUnprotect(this ISimpleTitle title, string reason)
		{
			var protection = new ProtectInputItem("create", ProtectionWord(ProtectionLevel.None)!);
			return title.NotNull(nameof(title)).Protect(reason, new[] { protection });
		}

		/// <summary>Deletes the title for the specified reason.</summary>
		/// <param name="title">The title to delete.</param>
		/// <param name="reason">The reason for the deletion.</param>
		/// <returns>A value indicating the change status of the block.</returns>
		public static ChangeStatus Delete(this ISimpleTitle title, string reason)
		{
			title.ThrowNull(nameof(title));
			reason.ThrowNull(nameof(reason));
			var site = title.Namespace.Site;
			var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				[nameof(reason)] = reason,
			};

			return site.PublishChange(site, parameters, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				var input = new DeleteInput(FullPageName(title)) { Reason = reason };
				var retval = title.Namespace.Site.AbstractionLayer.Delete(input);
				return retval.LogId == 0
					? ChangeStatus.Failure
					: ChangeStatus.Success;
			}
		}

		/// <summary>Protects the title.</summary>
		/// <param name="title">The title to protect.</param>
		/// <param name="reason">The reason for the protection.</param>
		/// <param name="editProtection">The edit-protection level.</param>
		/// <param name="moveProtection">The move-protection level.</param>
		/// <param name="expiry">The expiry date and time.</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		public static ChangeStatus Protect(this ISimpleTitle title, string reason, ProtectionLevel editProtection, ProtectionLevel moveProtection, DateTime expiry) => title.Protect(reason, ProtectionWord(editProtection), ProtectionWord(moveProtection), expiry);

		/// <summary>Protects the title.</summary>
		/// <param name="title">The title to protect.</param>
		/// <param name="reason">The reason for the protection.</param>
		/// <param name="editProtection">The edit-protection level.</param>
		/// <param name="moveProtection">The move-protection level.</param>
		/// <param name="duration">The duration of the protection (e.g., "2 weeks").</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		public static ChangeStatus Protect(this ISimpleTitle title, string reason, ProtectionLevel editProtection, ProtectionLevel moveProtection, string? duration) => title.Protect(reason, ProtectionWord(editProtection), ProtectionWord(moveProtection), duration);

		/// <summary>Protects the title.</summary>
		/// <param name="title">The title to protect.</param>
		/// <param name="reason">The reason for the protection.</param>
		/// <param name="editProtection">The edit-protection level.</param>
		/// <param name="moveProtection">The move-protection level.</param>
		/// <param name="expiry">The expiry date and time.</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		/// <remarks>title version allows custom protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="Protect(ISimpleTitle, string, ProtectionLevel, ProtectionLevel, DateTime)"/> version of title call.</remarks>
		public static ChangeStatus Protect(this ISimpleTitle title, string reason, string editProtection, string moveProtection, DateTime expiry)
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

			return title.NotNull(nameof(title)).Protect(reason, protections);
		}

		/// <summary>Protects the title.</summary>
		/// <param name="title">The title to protect.</param>
		/// <param name="reason">The reason for the protection.</param>
		/// <param name="editProtection">The edit-protection level.</param>
		/// <param name="moveProtection">The move-protection level.</param>
		/// <param name="duration">The duration of the protection (e.g., "2 weeks").</param>
		/// <returns>A value indicating the change status of the protection.</returns>
		/// <remarks>title version allows custom protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="Protect(ISimpleTitle, string, ProtectionLevel, ProtectionLevel, string)"/> version of title call.</remarks>
		public static ChangeStatus Protect(this ISimpleTitle title, string reason, string? editProtection, string? moveProtection, string? duration)
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

			return title.NotNull(nameof(title)).Protect(reason, protections);
		}

		/// <summary>Unprotects the title for the specified reason.</summary>
		/// <param name="title">The title to unprotect.</param>
		/// <param name="reason">The reason.</param>
		/// <param name="editUnprotect">if set to <see langword="true"/>, removes edit protection.</param>
		/// <param name="moveUnprotect">if set to <see langword="true"/>, removes move protection.</param>
		/// <returns>A value indicating the change status of the unprotection.</returns>
		public static ChangeStatus Unprotect(this ISimpleTitle title, string reason, bool editUnprotect, bool moveUnprotect) => title.Protect(
			reason,
			editUnprotect ? ProtectionLevel.None : ProtectionLevel.NoChange,
			moveUnprotect ? ProtectionLevel.None : ProtectionLevel.NoChange,
			null);
		#endregion

		#region Private Methods
		private static ChangeStatus Protect(this ISimpleTitle title, string reason, ICollection<ProtectInputItem> protections)
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

			return title.Namespace.Site.PublishChange(title, parameters, ChangeFunc);

			ChangeStatus ChangeFunc()
			{
				var input = new ProtectInput(FullPageName(title))
				{
					Protections = protections,
					Reason = reason
				};

				return Protect(title.Namespace.Site, input)
					? ChangeStatus.Success
					: ChangeStatus.Failure;
			}
		}

		#region Protected Methods

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
		#endregion

		// A dictionary is probably overkill for three items.
		private static string ProtectionWord(ProtectionLevel level) => level switch
		{
			ProtectionLevel.None => "all",
			ProtectionLevel.Semi => "autoconfirmed",
			ProtectionLevel.Full => "sysop",
			_ => throw new ArgumentOutOfRangeException(nameof(level))
		};
		#endregion
	}
}
