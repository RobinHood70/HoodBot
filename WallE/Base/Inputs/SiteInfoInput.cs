#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	#region Public Enumerations
	[Flags]
	public enum SiteInfoProperties
	{
		None = 0,
		General = 1 << 0,
		Namespaces = 1 << 1,
		NamespaceAliases = 1 << 2,
		SpecialPageAliases = 1 << 3,
		MagicWords = 1 << 4,
		InterwikiMap = 1 << 5,
		DbReplLag = 1 << 6,
		Statistics = 1 << 7,
		UserGroups = 1 << 8,
		Libraries = 1 << 9,
		Extensions = 1 << 10,
		FileExtensions = 1 << 11,
		RightsInfo = 1 << 12,
		Restrictions = 1 << 13,
		Languages = 1 << 14,
		Skins = 1 << 15,
		ExtensionTags = 1 << 16,
		FunctionHooks = 1 << 17,
		ShowHooks = 1 << 18,
		Variables = 1 << 19,
		Protocols = 1 << 20,
		DefaultOptions = 1 << 21,
		All = General | Namespaces | NamespaceAliases | SpecialPageAliases | MagicWords | InterwikiMap | DbReplLag | Statistics | UserGroups | Libraries | Extensions | FileExtensions | RightsInfo | Restrictions | Languages | Skins | ExtensionTags | FunctionHooks | ShowHooks | Variables | Protocols | DefaultOptions
	}
	#endregion

	public class SiteInfoInput
	{
		#region Constructors
		public SiteInfoInput(SiteInfoProperties properties) => this.Properties = properties;
		#endregion

		#region Public Properties
		public Filter FilterLocalInterwiki { get; set; }

		public string? InterwikiLanguageCode { get; set; }

		public SiteInfoProperties Properties { get; }

		public bool ShowAllDatabases { get; set; }

		public bool ShowNumberInGroup { get; set; }
		#endregion
	}
}
