#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

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
		#region Constructors
		internal SiteInfoResult(SiteInfoGeneral? general, IReadOnlyDictionary<string, object> defaultOptions, SiteInfoSkin? defaultSkin, IReadOnlyList<SiteInfoExtensions> extensions, IReadOnlyList<string> extensionTags, IReadOnlyList<string> fileExtensions, IReadOnlyList<string> functionHooks, IReadOnlyList<SiteInfoInterwikiMap> interwikiMap, IReadOnlyList<SiteInfoLag> lagInfo, IReadOnlyList<SiteInfoLanguage> languages, IReadOnlyList<SiteInfoLibrary> libraries, IReadOnlyList<SiteInfoMagicWord> magicWords, IReadOnlyList<SiteInfoNamespace> namespaces, IReadOnlyList<SiteInfoNamespaceAlias> namespaceAliases, IReadOnlyList<string> protocols, SiteInfoRestriction? restrictions, SiteInfoRights? rights, IReadOnlyList<SiteInfoSubscribedHook> subscribedHooks, IReadOnlyList<SiteInfoSkin> skins, IReadOnlyList<SiteInfoSpecialPageAlias> specialPageAliases, SiteInfoStatistics? statistics, IReadOnlyList<SiteInfoUserGroup> userGroups, IReadOnlyList<string> variables)
		{
			this.General = general;
			this.DefaultOptions = defaultOptions;
			this.DefaultSkin = defaultSkin;
			this.Extensions = extensions;
			this.ExtensionTags = extensionTags;
			this.FileExtensions = fileExtensions;
			this.FunctionHooks = functionHooks;
			this.InterwikiMap = interwikiMap;
			this.LagInfo = lagInfo;
			this.Languages = languages;
			this.Libraries = libraries;
			this.MagicWords = magicWords;
			this.Namespaces = namespaces;
			this.NamespaceAliases = namespaceAliases;
			this.Protocols = protocols;
			this.Restrictions = restrictions;
			this.Rights = rights;
			this.SubscribedHooks = subscribedHooks;
			this.Skins = skins;
			this.SpecialPageAliases = specialPageAliases;
			this.Statistics = statistics;
			this.UserGroups = userGroups;
			this.Variables = variables;
		}
		#endregion

		#region Public Properties

		// Because of the unusual way continuation works for SiteInfo (each module is kept whole, but different modules may be spread across responses), all properties are internally settable.
		public SiteInfoGeneral? General { get; internal set; }

		public IReadOnlyDictionary<string, object> DefaultOptions { get; internal set; }

		public SiteInfoSkin? DefaultSkin { get; internal set; }

		public IReadOnlyList<SiteInfoExtensions> Extensions { get; internal set; }

		public IReadOnlyList<string> ExtensionTags { get; internal set; }

		public IReadOnlyList<string> FileExtensions { get; internal set; }

		public IReadOnlyList<string> FunctionHooks { get; internal set; }

		public IReadOnlyList<SiteInfoInterwikiMap> InterwikiMap { get; internal set; }

		public IReadOnlyList<SiteInfoLag> LagInfo { get; internal set; }

		public IReadOnlyList<SiteInfoLanguage> Languages { get; internal set; }

		public IReadOnlyList<SiteInfoLibrary> Libraries { get; internal set; }

		public IReadOnlyList<SiteInfoMagicWord> MagicWords { get; internal set; }

		public IReadOnlyList<SiteInfoNamespace> Namespaces { get; internal set; }

		public IReadOnlyList<SiteInfoNamespaceAlias> NamespaceAliases { get; internal set; }

		public IReadOnlyList<string> Protocols { get; internal set; }

		public SiteInfoRestriction? Restrictions { get; internal set; }

		public SiteInfoRights? Rights { get; internal set; }

		/// <summary>Gets the list of subscribed hooks for the ShowHooks option.</summary>
		/// <value>A collection of subscribed hook information.</value>
		public IReadOnlyList<SiteInfoSubscribedHook> SubscribedHooks { get; internal set; }

		public IReadOnlyList<SiteInfoSkin> Skins { get; internal set; }

		public IReadOnlyList<SiteInfoSpecialPageAlias> SpecialPageAliases { get; internal set; }

		public SiteInfoStatistics? Statistics { get; internal set; }

		public IReadOnlyList<SiteInfoUserGroup> UserGroups { get; internal set; }

		public IReadOnlyList<string> Variables { get; internal set; }
		#endregion
	}
}
