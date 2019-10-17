﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;

	#region Public Enumerations
	[Flags]
	public enum SiteInfoFlags
	{
		None = 0,
		CaseSensitive = 1,
		ImageWhitelistEnabled = 1 << 1,
		LanguageConversion = 1 << 2,
		MiserMode = 1 << 3,
		ReadOnly = 1 << 4,
		RightToLeft = 1 << 5,
		TitleConversion = 1 << 6,
		WriteApi = 1 << 7
	}
	#endregion

	public class SiteInfoResult
	{
		#region Public Properties
		public string ArticlePath { get; set; }

		public string BasePage { get; set; }

		public string DbType { get; set; }

		public string DbVersion { get; set; }

		public IReadOnlyDictionary<string, object> DefaultOptions { get; set; } = ImmutableDictionary<string, object>.Empty;

		public SkinsItem DefaultSkin { get; set; } = new SkinsItem();

		public IReadOnlyList<ExtensionItem> Extensions { get; set; } = Array.Empty<ExtensionItem>();

		public IReadOnlyList<string> ExtensionTags { get; set; } = Array.Empty<string>();

		public IReadOnlyList<string> ExternalImages { get; set; } = Array.Empty<string>();

		public IReadOnlyList<string> FallbackLanguages { get; set; } = Array.Empty<string>();

		public string Fallback8BitEncoding { get; set; }

		public string Favicon { get; set; }

		public IReadOnlyList<string> FileExtensions { get; set; } = Array.Empty<string>();

		public SiteInfoFlags Flags { get; set; }

		public IReadOnlyList<string> FunctionHooks { get; set; } = Array.Empty<string>();

		public string Generator { get; set; }

		public string GitBranch { get; set; }

		public string GitHash { get; set; }

		public string HhvmVersion { get; set; }

		public IReadOnlyDictionary<string, ImageLimitsItem> ImageLimits { get; set; } = ImmutableDictionary<string, ImageLimitsItem>.Empty;

		public IReadOnlyList<InterwikiMapItem> InterwikiMap { get; set; } = Array.Empty<InterwikiMapItem>();

		public IReadOnlyList<LagItem> LagInfo { get; set; } = Array.Empty<LagItem>();

		public string Language { get; set; }

		public IReadOnlyList<LanguageItem> Languages { get; set; } = Array.Empty<LanguageItem>();

		public string LegalTitleChars { get; set; }

		public IReadOnlyList<LibrariesItem> Libraries { get; set; } = Array.Empty<LibrariesItem>();

		public string LinkPrefix { get; set; }

		public string LinkPrefixCharset { get; set; }

		public string LinkTrail { get; set; }

		public string Logo { get; set; }

		public IReadOnlyList<MagicWordsItem> MagicWords { get; set; } = Array.Empty<MagicWordsItem>();

		public string MainPage { get; set; }

		public long MaxUploadSize { get; set; }

		public IReadOnlyList<NamespacesItem> Namespaces { get; set; } = Array.Empty<NamespacesItem>();

		public IReadOnlyList<NamespaceAliasesItem> NamespaceAliases { get; set; } = Array.Empty<NamespaceAliasesItem>();

		public string PhpSapi { get; set; }

		public string PhpVersion { get; set; }

		public IReadOnlyList<string> Protocols { get; set; } = Array.Empty<string>();

		public bool ReadOnly { get; set; }

		public string ReadOnlyReason { get; set; }

		public RestrictionsItem Restrictions { get; set; } = new RestrictionsItem();

		public long Revision { get; set; }

		public string RightsText { get; set; }

		public string RightsUrl { get; set; }

		public string Script { get; set; }

		public string ScriptPath { get; set; }

		public string Server { get; set; }

		public string ServerName { get; set; }

		/// <summary>Gets or sets the list of subscribed hooks for the ShowHooks option.</summary>
		/// <value>A collection of subscribed hook information.</value>
		public IReadOnlyList<SubscribedHooksItem> SubscribedHooks { get; set; } = Array.Empty<SubscribedHooksItem>();

		public string SiteName { get; set; }

		public IReadOnlyList<SkinsItem> Skins { get; set; } = Array.Empty<SkinsItem>();

		public IReadOnlyList<SpecialPageAliasesItem> SpecialPageAliases { get; set; } = Array.Empty<SpecialPageAliasesItem>();

		public StatisticsInfo Statistics { get; set; } = new StatisticsInfo();

		public IReadOnlyDictionary<string, int> ThumbLimits { get; set; } = ImmutableDictionary<string, int>.Empty;

		public DateTime? Time { get; set; }

		public TimeSpan? TimeOffset { get; set; }

		public string TimeZone { get; set; }

		public IReadOnlyList<UserGroupsItem> UserGroups { get; set; } = Array.Empty<UserGroupsItem>();

		public IReadOnlyList<string> Variables { get; set; } = Array.Empty<string>();

		public string VariantArticlePath { get; set; }

		/// <summary>Gets or sets the list of language variants.</summary>
		/// <value>The list of language variants.</value>
		/// <remarks>Language names are <i>not</i> included, even when returned by the wiki. This is to keep this collection compatible with the related Fallback collection and because of the unlikelihood of ever needing that information in a bot.</remarks>
		public IReadOnlyList<string> Variants { get; set; } = Array.Empty<string>();

		public string WikiId { get; set; }
		#endregion
	}
}
