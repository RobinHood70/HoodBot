﻿namespace RobinHood70.Robby;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RobinHood70.CommonCode;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Properties;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Design;
using RobinHood70.WikiCommon.Properties;

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

	/// <summary>A non-standard protection level was found.</summary>
	Unknown,
}
#endregion

/// <summary>Provides a light-weight holder for titles with several information and manipulation functions.</summary>
public static class TitleExtensions
{
	#region Public Methods

	/// <summary>Protects a non-existent page from being created.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason for the create-protection.</param>
	/// <param name="createProtection">The protection level.</param>
	/// <param name="expiry">The expiry date and time.</param>
	/// <returns>A value indicating the change status of the protection.</returns>
	public static ChangeStatus CreateProtect(this Title title, string reason, ProtectionLevel createProtection, DateTime expiry) =>
		createProtection == ProtectionLevel.NoChange
			? ChangeStatus.NoEffect
			: title.CreateProtect(reason, ProtectionWord(createProtection), expiry);

	/// <summary>Protects a non-existent page from being created.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason for the create-protection.</param>
	/// <param name="createProtection">The protection level.</param>
	/// <param name="expiry">The expiry date and time.</param>
	/// <returns>A value indicating the change status of the protection.</returns>
	/// <remarks>title version allows custom create-protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="CreateProtect(Title, string, ProtectionLevel, DateTime)"/> version of title call.</remarks>
	public static ChangeStatus CreateProtect(this Title title, string reason, string? createProtection, DateTime expiry)
	{
		ArgumentNullException.ThrowIfNull(title);
		if (createProtection == null)
		{
			return ChangeStatus.NoEffect;
		}

		ProtectInputItem protection = new("create", createProtection) { Expiry = expiry };
		return Protect(title, reason, [protection]);
	}

	/// <summary>Protects a non-existent page from being created.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason for the create-protection.</param>
	/// <param name="createProtection">The protection level.</param>
	/// <param name="duration">The duration of the create-protection (e.g., "2 weeks").</param>
	/// <returns>A value indicating the change status of the protection.</returns>
	public static ChangeStatus CreateProtect(this Title title, string reason, ProtectionLevel createProtection, string duration) =>
		createProtection == ProtectionLevel.NoChange
			? ChangeStatus.NoEffect
			: title.CreateProtect(reason, ProtectionWord(createProtection)!, duration);

	/// <summary>Protects a non-existent page from being created.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason for the create-protection.</param>
	/// <param name="createProtection">The protection level.</param>
	/// <param name="duration">The duration of the create-protection (e.g., "2 weeks").</param>
	/// <returns>A value indicating the change status of the protection.</returns>
	/// <remarks>title version allows custom create-protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="CreateProtect(Title, string, ProtectionLevel, string)"/> version of title call.</remarks>
	public static ChangeStatus CreateProtect(this Title title, string reason, string createProtection, string duration)
	{
		ArgumentNullException.ThrowIfNull(title);
		ArgumentNullException.ThrowIfNull(createProtection);
		ProtectInputItem protection = new("create", createProtection) { ExpiryRelative = duration };
		return Protect(title, reason, [protection]);
	}

	/// <summary>Unprotects the title.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason for the unprotection.</param>
	/// <returns>A value indicating the change status of the unprotection.</returns>
	public static ChangeStatus CreateUnprotect(this Title title, string reason)
	{
		ArgumentNullException.ThrowIfNull(title);
		ProtectInputItem protection = new("create", ProtectionWord(ProtectionLevel.None)!);
		return Protect(title, reason, [protection]);
	}

	/// <summary>Deletes the title for the specified reason.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason for the deletion.</param>
	/// <returns>A value indicating the change status of the block.</returns>
	public static ChangeStatus Delete(this Title title, string reason)
	{
		ArgumentNullException.ThrowIfNull(title);
		ArgumentNullException.ThrowIfNull(reason);
		var site = title.Namespace.Site;
		Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
		{
			[nameof(title)] = title,
			[nameof(reason)] = reason,
		};

		return site.PublishChange(site, parameters, ChangeFunc);

		ChangeStatus ChangeFunc()
		{
			DeleteInput input = new(title.FullPageName()) { Reason = reason };
			try
			{
				// Failure always throws and the return value has nothing else of interest in it.
				_ = title.Site.AbstractionLayer.Delete(input);
				return ChangeStatus.Success;
			}
			catch (WikiException)
			{
				return ChangeStatus.Failure;
			}
		}
	}

	/// <summary>Returns a value indicating if the page exists. This will trigger a Load operation.</summary>
	/// <param name="title">The title to work on.</param>
	/// <returns><see langword="true" /> if the page exists; otherwise <see langword="false" />.</returns>
	public static bool Exists(this Title title) => title.Load(PageModules.None, false).Exists;

	/// <summary>Loads the page found at this title.</summary>
	/// <param name="title">The title to work on.</param>
	/// <returns>A page for this title. Can be null if the title is a Special or Media page.</returns>
	public static Page Load(this Title title) => title.Load(PageLoadOptions.Default);

	/// <summary>Loads the page found at this title.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="modules">The modules to load.</param>
	/// <param name="followRedirects">Indicates whether redirects should be followed when loading.</param>
	/// <returns>A page for this title. Can be null if the title is a Special or Media page.</returns>
	public static Page Load(this Title title, PageModules modules, bool followRedirects) => title.Load(new PageLoadOptions(modules, followRedirects));

	/// <summary>Loads the page found at this title.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="options">The page load options.</param>
	/// <returns>A page for this title.</returns>
	public static Page Load(this Title title, PageLoadOptions options)
	{
		ArgumentNullException.ThrowIfNull(title);
		if (title.Namespace.CanTalk)
		{
			var pages = PageCollection.Unlimited(title.Site, options);
			pages.GetTitles(title.FullPageName());
			if (pages.Count == 1)
			{
				return pages[0];
			}
		}

		throw new InvalidOperationException(Globals.CurrentCulture(Resources.PageCouldNotBeLoaded, title.FullPageName()));
	}

	/// <summary>Protects the title.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason for the protection.</param>
	/// <param name="editProtection">The edit-protection level.</param>
	/// <param name="moveProtection">The move-protection level.</param>
	/// <param name="expiry">The expiry date and time.</param>
	/// <returns>A value indicating the change status of the protection.</returns>
	public static ChangeStatus Protect(this Title title, string reason, ProtectionLevel editProtection, ProtectionLevel moveProtection, DateTime expiry) => title.Protect(reason, ProtectionWord(editProtection), ProtectionWord(moveProtection), expiry);

	/// <summary>Protects the title.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason for the protection.</param>
	/// <param name="editProtection">The edit-protection level.</param>
	/// <param name="moveProtection">The move-protection level.</param>
	/// <param name="duration">The duration of the protection (e.g., "2 weeks").</param>
	/// <returns>A value indicating the change status of the protection.</returns>
	public static ChangeStatus Protect(this Title title, string reason, ProtectionLevel editProtection, ProtectionLevel moveProtection, string? duration) => title.Protect(reason, ProtectionWord(editProtection), ProtectionWord(moveProtection), duration);

	/// <summary>Protects the title.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason for the protection.</param>
	/// <param name="editProtection">The edit-protection level.</param>
	/// <param name="moveProtection">The move-protection level.</param>
	/// <param name="expiry">The expiry date and time.</param>
	/// <returns>A value indicating the change status of the protection.</returns>
	/// <remarks>title version allows custom protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="Protect(Title, string, ProtectionLevel, ProtectionLevel, DateTime)"/> version of title call.</remarks>
	public static ChangeStatus Protect(this Title title, string reason, string? editProtection, string? moveProtection, DateTime expiry)
	{
		ArgumentNullException.ThrowIfNull(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(reason);

		var wikiExpiry = expiry == DateTime.MaxValue ? null : (DateTime?)expiry;
		var protections = new List<ProtectInputItem>(2);
		if (editProtection is not null)
		{
			protections.Add(new ProtectInputItem("edit", editProtection) { Expiry = wikiExpiry });
		}

		if (moveProtection is not null)
		{
			protections.Add(new ProtectInputItem("move", moveProtection) { Expiry = wikiExpiry });
		}

		return Protect(title, reason, protections);
	}

	/// <summary>Protects the title.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason for the protection.</param>
	/// <param name="editProtection">The edit-protection level.</param>
	/// <param name="moveProtection">The move-protection level.</param>
	/// <param name="duration">The duration of the protection (e.g., "2 weeks").</param>
	/// <returns>A value indicating the change status of the protection.</returns>
	/// <remarks>title version allows custom protection values for wikis that have added protection levels beyond the default. For a wiki with the default setup, use the <see cref="Protect(Title, string, ProtectionLevel, ProtectionLevel, string?)"/> version of title call.</remarks>
	public static ChangeStatus Protect(this Title title, string reason, string? editProtection, string? moveProtection, string? duration)
	{
		ArgumentNullException.ThrowIfNull(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(reason);

		duration ??= "infinite";

		var protections = new List<ProtectInputItem>(2);
		if (editProtection != null)
		{
			protections.Add(new ProtectInputItem("edit", editProtection) { ExpiryRelative = duration });
		}

		if (moveProtection != null)
		{
			protections.Add(new ProtectInputItem("move", moveProtection) { ExpiryRelative = duration });
		}

		return Protect(title, reason, protections);
	}

	/// <summary>Unprotects the title for the specified reason.</summary>
	/// <param name="title">The title to work on.</param>
	/// <param name="reason">The reason.</param>
	/// <param name="editUnprotect">if set to <see langword="true"/>, removes edit protection.</param>
	/// <param name="moveUnprotect">if set to <see langword="true"/>, removes move protection.</param>
	/// <returns>A value indicating the change status of the unprotection.</returns>
	public static ChangeStatus Unprotect(this Title title, string reason, bool editUnprotect, bool moveUnprotect) => title.Protect(
		reason,
		editUnprotect ? ProtectionLevel.None : ProtectionLevel.NoChange,
		moveUnprotect ? ProtectionLevel.None : ProtectionLevel.NoChange,
		null);
	#endregion

	#region Private Methods

	/// <summary>Protects the title based on the specified input.</summary>
	/// <param name="site">The site the title is on.</param>
	/// <param name="input">The input.</param>
	/// <returns><see langword="true"/> if all protections were set to the specified values.</returns>
	private static bool Protect(Site site, ProtectInput input)
	{
		ArgumentNullException.ThrowIfNull(input);
		Globals.ThrowIfNull(input.Protections, nameof(input), nameof(input.Protections));
		var inputCount = new List<ProtectInputItem>(input.Protections).Count;
		var result = site.AbstractionLayer.Protect(input);

		// This is a simple count comparison, because matching up inputs and outputs would be more intensive and should not be necessary, barring any breaking changes to the MediaWiki protection algorithm.
		return result.Protections.Count == inputCount;
	}

	private static ChangeStatus Protect([NotNull][ValidatedNotNull] Title title, string reason, List<ProtectInputItem> protections)
	{
		ArgumentNullException.ThrowIfNull(reason);
		ArgumentNullException.ThrowIfNull(protections);
		if (protections.Count == 0)
		{
			return ChangeStatus.NoEffect;
		}

		Dictionary<string, object?> parameters = new(StringComparer.Ordinal)
		{
			[nameof(reason)] = reason,
			[nameof(protections)] = protections,
		};

		return title.Site.PublishChange(title, parameters, ChangeFunc);

		ChangeStatus ChangeFunc()
		{
			ProtectInput input = new(title.FullPageName())
			{
				Protections = protections,
				Reason = reason
			};

			return Protect(title.Site, input)
				? ChangeStatus.Success
				: ChangeStatus.Failure;
		}
	}

	// A dictionary is probably overkill for three items.
	private static string? ProtectionWord(ProtectionLevel level) => level switch
	{
		ProtectionLevel.None => "all",
		ProtectionLevel.Semi => "autoconfirmed",
		ProtectionLevel.Full => "sysop",
		ProtectionLevel.NoChange => null,
		_ => throw new ArgumentOutOfRangeException(paramName: nameof(level), message: GlobalMessages.InvalidSwitchValue)
	};
	#endregion
}