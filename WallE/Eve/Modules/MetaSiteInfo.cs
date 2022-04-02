namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics.CodeAnalysis;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	[SuppressMessage("Maintainability", "CA1506:Avoid excessive class coupling", Justification = "SiteInfo is inherently complex.")]
	internal sealed class MetaSiteInfo : QueryModule<SiteInfoInput, SiteInfoResult>, IContinuableQueryModule
	{
		#region Constructors
		public MetaSiteInfo(WikiAbstractionLayer wal, SiteInfoInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override string ContinueName => "prop";

		// This module can continue, but does not support limits in any way, so override normal handling and always continue if asked to.
		public override bool ContinueParsing => true;

		public override int MinimumVersion => 0;

		public override string Name => "siteinfo";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType => "meta";

		protected override string Prefix => "si";
		#endregion

		#region Public Override Methods
		public override bool HandleWarning(string from, string? text)
		{
			if (this.SiteVersion == 0 && string.Equals(from, "main", StringComparison.Ordinal) && text?.Contains("formatversion", StringComparison.Ordinal) == true)
			{
				this.Wal.DetectedFormatVersion = 1;
				return true;
			}

			return base.HandleWarning(from, text);
		}
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, SiteInfoInput input)
		{
			var prop = input.NotNull().Properties;
			if (this.SiteVersion != 0)
			{
				prop = FlagFilter
					.Check(this.SiteVersion, prop)
					.FilterBefore(118, SiteInfoProperties.ExtensionTags | SiteInfoProperties.FunctionHooks | SiteInfoProperties.ShowHooks | SiteInfoProperties.Skins)
					.FilterBefore(120, SiteInfoProperties.Variables)
					.FilterBefore(121, SiteInfoProperties.Protocols)
					.FilterBefore(123, SiteInfoProperties.DefaultOptions | SiteInfoProperties.Restrictions)
					.FilterBefore(125, SiteInfoProperties.Libraries)
					.Value;
			}

			request
				.NotNull()
				.AddFlags("prop", prop)
				.AddFilterPipedIf("filteriw", "local", input.FilterLocalInterwiki, (prop & SiteInfoProperties.InterwikiMap) != 0)
				.AddIf("showalldb", input.ShowAllDatabases, (prop & SiteInfoProperties.DbReplLag) != 0)
				.AddIf("numberingroup", input.ShowNumberInGroup, (prop & SiteInfoProperties.UserGroups) != 0)
				.AddIfNotNullIf("inlanguagecode", input.InterwikiLanguageCode, (prop & SiteInfoProperties.InterwikiMap) != 0);
		}

		protected override void DeserializeParent(JToken parent)
		{
			var (defaultSkin, skins) = GetSkins(parent.NotNull());
			SiteInfoResult output = new(
				general: this.GetGeneral(parent),
				defaultOptions: GetDefaultOptions(parent),
				defaultSkin: defaultSkin,
				extensions: this.GetExtensions(parent),
				extensionTags: GetExtensionTags(parent),
				fileExtensions: GetFileExtensions(parent),
				functionHooks: GetFunctionHooks(parent),
				interwikiMap: GetInterwikiMap(parent),
				lagInfo: GetDbReplLag(parent),
				languages: GetLanguages(parent),
				libraries: GetLibraries(parent),
				magicWords: GetMagicWords(parent),
				namespaces: GetNamespaces(parent),
				namespaceAliases: GetNamespaceAliases(parent),
				protocols: GetProtocols(parent),
				restrictions: GetRestrictions(parent),
				rights: GetRightsInfo(parent),
				subscribedHooks: GetSubscribedHooks(parent),
				skins: skins,
				specialPageAliases: GetSpecialPageAliases(parent),
				statistics: GetStatistics(parent),
				userGroups: GetUserGroups(parent),
				variables: GetVariables(parent));

			this.Output = output;
		}

		protected override void DeserializeResult(JToken? result) => throw new InvalidOperationException(EveMessages.CannotDeserialize);
		#endregion

		#region Private Static Methods
		private static IEnumerable<(string Key, JToken Value)> GetBCIndexedList(JToken? token, int formatVersion)
		{
			if (token == null)
			{
				yield break;
			}

			if (formatVersion == 2)
			{
				foreach (var node in token)
				{
					if (node is JProperty useNode)
					{
						yield return (useNode.Name, useNode.Value);
					}
				}
			}
			else
			{
				var counter = 0;
				foreach (var node in token)
				{
					yield return (counter.ToStringInvariant(), node);
					counter++;
				}
			}
		}

		private static IReadOnlyList<SiteInfoLag> GetDbReplLag(JToken parent)
		{
			if (parent["dbrepllag"] is not JToken node)
			{
				return Array.Empty<SiteInfoLag>();
			}

			List<SiteInfoLag> retval = new();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoLag(result.MustHaveString("host"), (int)result.MustHave("lag")));
			}

			return retval;
		}

		private static IReadOnlyDictionary<string, object> GetDefaultOptions(JToken parent) => parent["defaultoptions"] is JToken node ? node.GetStringDictionary<object>() : ImmutableDictionary<string, object>.Empty;

		private static IReadOnlyList<string> GetExtensionTags(JToken parent) => parent["extensiontags"] is JToken node ? node.GetList<string>() : Array.Empty<string>();

		private static IReadOnlyList<string> GetFileExtensions(JToken parent)
		{
			if (parent["fileextensions"] is not JToken node)
			{
				return Array.Empty<string>();
			}

			List<string> retval = new();
			foreach (var result in node)
			{
				retval.Add(result.MustHaveString("ext"));
			}

			return retval;
		}

		private static IReadOnlyList<string> GetFunctionHooks(JToken parent) => parent["functionhooks"] is JToken node ? node.GetList<string>() : Array.Empty<string>();

		private static IReadOnlyList<SiteInfoInterwikiMap> GetInterwikiMap(JToken parent)
		{
			if (parent["interwikimap"] is not JToken node)
			{
				return Array.Empty<SiteInfoInterwikiMap>();
			}

			List<SiteInfoInterwikiMap> retval = new();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoInterwikiMap(
					prefix: result.MustHaveString("prefix"),
					url: result.MustHaveString("url"),
					apiUrl: (string?)result["api"],
					flags: result.GetFlags(
						("extralanguagelink", InterwikiMapFlags.ExtraLanguageLink),
						("local", InterwikiMapFlags.Local),
						("localinterwiki", InterwikiMapFlags.LocalInterwiki),
						("protorel", InterwikiMapFlags.ProtocolRelative),
						("trans", InterwikiMapFlags.TransclusionAllowed)),
					language: (string?)result["language"],
					linkText: (string?)result["linktext"],
					siteName: (string?)result["sitename"],
					wikiId: (string?)result["wikiid"]));
			}

			return retval;
		}

		private static IReadOnlyList<SiteInfoLanguage> GetLanguages(JToken parent)
		{
			if (parent["languages"] is not JToken node)
			{
				return Array.Empty<SiteInfoLanguage>();
			}

			List<SiteInfoLanguage> retval = new();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoLanguage(result.MustHaveString("code"), result.MustHaveBCString("name")));
			}

			return retval;
		}

		private static IReadOnlyList<SiteInfoLibrary> GetLibraries(JToken parent)
		{
			if (parent["libraries"] is not JToken node)
			{
				return Array.Empty<SiteInfoLibrary>();
			}

			List<SiteInfoLibrary> retval = new();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoLibrary(
					name: result.MustHaveString("name"),
					version: result.MustHaveString("version")));
			}

			return retval;
		}

		private static IReadOnlyList<SiteInfoMagicWord> GetMagicWords(JToken parent)
		{
			if (parent["magicwords"] is not JToken node)
			{
				return Array.Empty<SiteInfoMagicWord>();
			}

			List<SiteInfoMagicWord> retval = new();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoMagicWord(
					name: result.MustHaveString("name"),
					aliases: result.MustHaveList<string>("aliases"),
					caseSensitive: result["case-sensitive"].GetBCBool()));
			}

			return retval;
		}

		private static IReadOnlyList<SiteInfoNamespaceAlias> GetNamespaceAliases(JToken parent)
		{
			if (parent["namespacealiases"] is not JToken node)
			{
				return Array.Empty<SiteInfoNamespaceAlias>();
			}

			List<SiteInfoNamespaceAlias> retval = new();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoNamespaceAlias(
					id: (int)result.MustHave("id"),
					alias: result.MustHaveBCString("alias")));
			}

			return retval;
		}

		private static IReadOnlyList<SiteInfoNamespace> GetNamespaces(JToken parent)
		{
			if (parent["namespaces"] is not JToken node)
			{
				return Array.Empty<SiteInfoNamespace>();
			}

			List<SiteInfoNamespace> retval = new();
			foreach (var resultNode in node)
			{
				if (resultNode.First is JToken result)
				{
					var id = (int)result.MustHave("id");
					var flags = result.GetFlags(
						("content", NamespaceFlags.ContentSpace),
						("nonincludable", NamespaceFlags.NonIncludable),
						("subpages", NamespaceFlags.AllowsSubpages))
						.AddCaseFlag(result, NamespaceFlags.CaseSensitive);

					if (id >= MediaWikiNamespaces.Main)
					{
						flags |= NamespaceFlags.CanTalk;
						if (id is MediaWikiNamespaces.Category or MediaWikiNamespaces.File)
						{
							flags |= NamespaceFlags.ForcedLinkSpace;
						}
					}

					retval.Add(new SiteInfoNamespace(
						id: id,
						canonicalName: (string?)result["canonical"] ?? string.Empty,
						defaultContentModel: (string?)result["defaultcontentmodel"],
						flags: flags,
						name: result.MustHaveBCString("name")));
				}
			}

			return retval;
		}

		private static IReadOnlyList<string> GetProtocols(JToken parent) => parent["protocols"] is JToken node ? node.GetList<string>() : Array.Empty<string>();

		private static SiteInfoRestriction? GetRestrictions(JToken parent) => parent["restrictions"] is JToken node
			? new SiteInfoRestriction(
				cascadingLevels: node["cascadinglevels"].GetList<string>(),
				levels: node["levels"].GetList<string>(),
				semiProtectedLevels: node["semiprotectedlevels"].GetList<string>(),
				types: node["types"].GetList<string>())
			: null;

		private static SiteInfoRights? GetRightsInfo(JToken parent) => parent["rightsinfo"] is JToken node ? new SiteInfoRights((string?)node["text"], (string?)node["url"]) : null;

		private static (SiteInfoSkin? DefaultSkin, IReadOnlyList<SiteInfoSkin> Skins) GetSkins(JToken parent)
		{
			if (parent["skins"] is not JToken node)
			{
				return (null, Array.Empty<SiteInfoSkin>());
			}

			SiteInfoSkin? defaultSkin = null;
			List<SiteInfoSkin>? retval = new();
			foreach (var result in node)
			{
				SiteInfoSkin item = new(
					code: result.MustHaveString("code"),
					name: result.MustHaveBCString("name"),
					unusable: result["unusable"].GetBCBool());
				if (result["default"].GetBCBool())
				{
					defaultSkin = item;
				}

				retval.Add(item);
			}

			return (defaultSkin, retval);
		}

		private static IReadOnlyList<SiteInfoSpecialPageAlias> GetSpecialPageAliases(JToken parent)
		{
			if (parent["specialpagealiases"] is not JToken node)
			{
				return Array.Empty<SiteInfoSpecialPageAlias>();
			}

			List<SiteInfoSpecialPageAlias> retval = new();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoSpecialPageAlias(
					realName: result.MustHaveString("realname"),
					aliases: result.MustHaveList<string>("aliases")));
			}

			return retval;
		}

		private static SiteInfoStatistics? GetStatistics(JToken parent) => parent["statistics"] is JToken node
			? new SiteInfoStatistics(
				activeUsers: (long)node.MustHave("activeusers"),
				admins: (long)node.MustHave("admins"),
				articles: (long)node.MustHave("articles"),
				edits: (long)node.MustHave("edits"),
				images: (long)node.MustHave("images"),
				jobs: (long)node.MustHave("jobs"),
				pages: (long)node.MustHave("pages"),
				users: (long)node.MustHave("users"),
				views: (long?)node["views"] ?? -1)
			: null;

		private static IReadOnlyList<SiteInfoSubscribedHook> GetSubscribedHooks(JToken parent)
		{
			if (parent["showhooks"] is not JToken node)
			{
				return Array.Empty<SiteInfoSubscribedHook>();
			}

			List<SiteInfoSubscribedHook> retval = new();
			foreach (var result in node)
			{
				// See https://phabricator.wikimedia.org/T117022 for details on the Scribunto check.
				var subscribersNode = result.MustHave("subscribers");
				var subscribers = subscribersNode.Type == JTokenType.Object && subscribersNode["scribunto"] is JToken scribuntoNode
					? new List<string> { (string?)scribuntoNode ?? string.Empty }
					: subscribersNode.GetList<string>();

				retval.Add(new SiteInfoSubscribedHook(
					name: result.MustHaveString("name"),
					subscribers: subscribers));
			}

			return retval;
		}

		private static IReadOnlyList<SiteInfoUserGroup> GetUserGroups(JToken parent)
		{
			if (parent["usergroups"] is not JToken node)
			{
				return Array.Empty<SiteInfoUserGroup>();
			}

			List<SiteInfoUserGroup> retval = new();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoUserGroup(
					name: result.MustHaveString("name"),
					rights: result.MustHaveList<string>("rights"),
					number: (long?)result["number"] ?? -1,
					add: result["add"].GetList<string>(),
					addSelf: result["add-self"].GetList<string>(),
					remove: result["remove"].GetList<string>(),
					removeSelf: result["remove-self"].GetList<string>()));
			}

			return retval;
		}

		private static IReadOnlyList<string> GetVariables(JToken parent) => parent["variables"] is JToken node ? node.GetList<string>() : Array.Empty<string>();
		#endregion

		#region Private Methods
		private IReadOnlyList<SiteInfoExtensions> GetExtensions(JToken parent)
		{
			if (parent["extensions"] is not JToken node)
			{
				return Array.Empty<SiteInfoExtensions>();
			}

			List<SiteInfoExtensions> retval = new();
			foreach (var result in node)
			{
				var name = (string?)result["name"];
				IReadOnlyList<string>? descMsgParams = null;
				try
				{
					descMsgParams = result["descriptionmsgparams"].GetList<string>();
				}
				catch (InvalidCastException)
				{
					this.Wal.AddWarning("siteinfo-unhandledparams", Globals.CurrentCulture(EveMessages.UnhandledParams, name ?? Globals.Unknown));
				}

				retval.Add(new SiteInfoExtensions(
					type: result.MustHaveString("type"),
					author: (string?)result["author"],
					credits: (string?)result["credits"],
					description: (string?)result["description"],
					descriptionMessage: (string?)result["descriptionmsg"],
					descriptionMessageParameters: descMsgParams,
					license: (string?)result["license"],
					licenseName: (string?)result["license-name"],
					name: name,
					nameMessage: (string?)result["namemsg"],
					url: (string?)result["url"],
					version: (string?)result["version"],
					versionControlSystem: (string?)result["vcs-system"],
					versionControlSystemDate: (DateTime?)result["vcs-date"],
					versionControlSystemUrl: (string?)result["vcs-url"],
					versionControlSystemVersion: (string?)result["vcs-version"]));
			}

			return retval;
		}

		private SiteInfoGeneral? GetGeneral(JToken parent)
		{
			// This is the one module that supports every version of the API, in order to allow at least basic info to be retrieved without error.
			if (parent["general"] is not JToken node)
			{
				return null;
			}

			List<string> fallback = new();
			var fallbacksNode = node["fallback"];
			if (fallbacksNode != null)
			{
				foreach (var token in fallbacksNode)
				{
					fallback.Add(token.MustHaveString("code"));
				}
			}

			Dictionary<string, ImageLimitsItem> imageLimits = new(StringComparer.Ordinal);
			if (node["imagelimits"] is JToken imageLimitsNode)
			{
				foreach (var (key, value) in GetBCIndexedList(imageLimitsNode, this.Wal.DetectedFormatVersion))
				{
					imageLimits.Add(key, new ImageLimitsItem((int)value.MustHave("width"), (int)value.MustHave("height")));
				}
			}

			var server = (string?)node["server"] ?? string.Empty;
			var serverName = (string?)node["servername"];
			if (string.IsNullOrEmpty(serverName) && !string.IsNullOrEmpty(server))
			{
				// Same approach as MediaWiki uses in Setup.php
				var canonical = server.StartsWith("//", StringComparison.Ordinal) ? "http:" + server : server;
				Uri uri = new(canonical);
				serverName = uri.Host;
			}

			Dictionary<string, int> thumbLimits = new(StringComparer.Ordinal);
			if (node["thumblimits"] is JToken thumbLimitsNode)
			{
				foreach (var (key, value) in GetBCIndexedList(thumbLimitsNode, this.Wal.DetectedFormatVersion))
				{
					thumbLimits.Add(key, (int)value);
				}
			}

			var timeOffsetNode = (double)node.MustHave("timeoffset");
			var timeOffset = TimeSpan.FromMinutes(timeOffsetNode);

			string? variantArticlePath = null;
			if (node["variantarticlepath"] is JToken variantArticlePathNode)
			{
				// Default value is "false", which gets emitted in JSON, so check for that.
				variantArticlePath = variantArticlePathNode?.Type == JTokenType.Boolean ? null : (string?)variantArticlePathNode;
			}

			List<string> variants = new();
			if (node["variants"] is JToken variantsNode)
			{
				foreach (var token in variantsNode)
				{
					variants.Add(token.MustHaveString("code"));
				}
			}

			return new SiteInfoGeneral(
				articlePath: (string?)node["articlepath"] ?? string.Empty,
				basePage: node.MustHaveString("base"),
				dbType: (string?)node["dbtype"] ?? string.Empty,
				dbVersion: (string?)node["dbversion"] ?? string.Empty,
				externalImages: node["externalimages"].GetList<string>(),
				fallback8BitEncoding: (string?)node["fallback8bitEncoding"] ?? string.Empty,
				fallbackLanguages: fallback,
				favicon: (string?)node["favicon"],
				flags: node.GetFlags(
					("imagewhitelistenabled", SiteInfoFlags.ImageWhitelistEnabled),
					("langconversion", SiteInfoFlags.LanguageConversion),
					("misermode", SiteInfoFlags.MiserMode),
					("readonly", SiteInfoFlags.ReadOnly),
					("righttoleft", SiteInfoFlags.RightToLeft),
					("titleconversion", SiteInfoFlags.TitleConversion),
					("writeapi", SiteInfoFlags.WriteApi))
					.AddCaseFlag(node, SiteInfoFlags.CaseSensitive),
				generator: node.MustHaveString("generator"),
				gitBranch: (string?)node["git-branch"],
				gitHash: (string?)node["git-hash"],
				hhvmVersion: (string?)node["hhvmversion"],
				imageLimits: imageLimits,
				language: (string?)node["lang"] ?? string.Empty,
				legalTitleChars: (string?)node["legaltitlechars"],
				linkPrefix: (string?)node["linkprefix"],
				linkPrefixCharset: (string?)node["linkprefixcharset"],
				linkTrail: (string?)node["linktrail"],
				logo: (string?)node["logo"],
				mainPage: node.MustHaveString("mainpage"),
				maxUploadSize: (long?)node["maxuploadsize"] ?? 0,
				phpSapi: (string?)node["phpsapi"] ?? string.Empty,
				phpVersion: (string?)node["phpversion"] ?? string.Empty,
				readOnlyReason: (string?)node["readonlyreason"],
				revision: (long?)node["revision"] ?? 0,
				script: (string?)node["script"] ?? string.Empty,
				scriptPath: (string?)node["scriptpath"] ?? string.Empty,
				server: server,
				serverName: serverName,
				siteName: node.MustHaveString("sitename"),
				thumbLimits: thumbLimits,
				time: (DateTime?)node["time"] ?? DateTime.Now,
				timeOffset: timeOffset,
				timeZone: (string?)node["timezone"] ?? string.Empty,
				variantArticlePath: variantArticlePath,
				variants: variants,
				wikiId: (string?)node["wikiid"] ?? string.Empty);
		}
		#endregion
	}
}