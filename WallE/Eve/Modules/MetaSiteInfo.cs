﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class MetaSiteInfo : QueryModule<SiteInfoInput, SiteInfoResult>, IContinuableQueryModule
	{
		#region Constructors
		public MetaSiteInfo(WikiAbstractionLayer wal, SiteInfoInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override string ContinueName { get; } = "prop";

		// This module can continue, but does not support limits in any way, so override normal handling and always continue if asked to.
		public override bool ContinueParsing => true;

		public override int MinimumVersion { get; } = 0;

		public override string Name { get; } = "siteinfo";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType { get; } = "meta";

		protected override string Prefix { get; } = "si";
		#endregion

		#region Public Override Methods
		public override bool HandleWarning(string from, string text)
		{
			if (this.SiteVersion == 0 && from == "main" && text.Contains("formatversion") == true)
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
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			var prop = input.Properties;
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
				.AddFlags("prop", prop)
				.AddFilterPipedIf("filteriw", "local", input.FilterLocalInterwiki, prop.HasFlag(SiteInfoProperties.InterwikiMap))
				.AddIf("showalldb", input.ShowAllDatabases, prop.HasFlag(SiteInfoProperties.DbReplLag))
				.AddIf("numberingroup", input.ShowNumberInGroup, prop.HasFlag(SiteInfoProperties.UserGroups))
				.AddIfNotNullIf("inlanguagecode", input.InterwikiLanguageCode, prop.HasFlag(SiteInfoProperties.InterwikiMap));
		}

		protected override void DeserializeParent(JToken parent)
		{
			ThrowNull(parent, nameof(parent));

			// Because this module can continue in non-standard fashion (each module will either appear in whole or not at all), we need to ensure that outputs are only written to if necessary.
			var (defaultSkin, skins) = GetSkins(parent);
			this.Output ??= new SiteInfoResult();
			var output = this.Output; // Mostly done to reduce hits on this.Output in reference search, since we're trying to limit using it whenever possible.
			output!.DefaultOptions ??= GetDefaultOptions(parent);
			output.DefaultSkin ??= defaultSkin;
			output.Extensions ??= this.GetExtensions(parent);
			output.ExtensionTags ??= GetExtensionTags(parent);
			output.FileExtensions ??= GetFileExtensions(parent);
			output.FunctionHooks ??= GetFunctionHooks(parent);
			output.General ??= this.GetGeneral(parent);
			output.InterwikiMap ??= GetInterwikiMap(parent);
			output.LagInfo ??= GetDbReplLag(parent);
			output.Languages ??= GetLanguages(parent);
			output.Libraries ??= GetLibraries(parent);
			output.MagicWords ??= GetMagicWords(parent);
			output.Namespaces ??= GetNamespaces(parent);
			output.NamespaceAliases ??= GetNamespaceAliases(parent);
			output.Protocols ??= GetProtocols(parent);
			output.Restrictions ??= GetRestrictions(parent);
			output.Rights ??= this.GetRightsInfo(parent);
			output.Skins ??= skins;
			output.SpecialPageAliases ??= GetSpecialPageAliases(parent);
			output.Statistics ??= GetStatistics(parent);
			output.SubscribedHooks ??= GetSubscribedHooks(parent);
			output.UserGroups ??= GetUserGroups(parent);
			output.Variables ??= GetVariables(parent);
		}

		protected override void DeserializeResult(JToken result) => throw new InvalidOperationException(EveMessages.CannotDeserialize);
		#endregion

		#region Private Static Methods
		private static List<SiteInfoLag>? GetDbReplLag(JToken parent)
		{
			if (!(parent["dbrepllag"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoLag>();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoLag(result.MustHaveString("host"), (int)result.MustHave("lag")));
			}

			return retval;
		}

		private static Dictionary<string, object>? GetDefaultOptions(JToken parent) => parent["defaultoptions"] is JToken node ? node.ToStringDictionary<object>() : null;

		private static List<string>? GetExtensionTags(JToken parent) => parent["extensiontags"] is JToken node ? node.ToList<string>() : null;

		private static List<string>? GetFileExtensions(JToken parent)
		{
			if (!(parent["fileextensions"] is JToken node))
			{
				return null;
			}

			var retval = new List<string>();
			foreach (var result in node)
			{
				retval.Add(result.MustHaveString("ext"));
			}

			return retval;
		}

		private static List<string>? GetFunctionHooks(JToken parent) => parent["functionhooks"] is JToken node ? node.ToList<string>() : null;

		private static List<SiteInfoInterwikiMap>? GetInterwikiMap(JToken parent)
		{
			if (!(parent["interwikimap"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoInterwikiMap>();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoInterwikiMap(
					prefix: result.MustHaveString("prefix"),
					url: result.MustHaveString("url"),
					flags: result.GetFlags(
						("extralanguagelink", InterwikiMapFlags.ExtraLanguageLink),
						("local", InterwikiMapFlags.Local),
						("localinterwiki", InterwikiMapFlags.LocalInterwiki),
						("protorel", InterwikiMapFlags.ProtocolRelative),
						("trans", InterwikiMapFlags.TransclusionAllowed)),
					language: (string?)result["language"],
					linkText: (string?)result["linktext"],
					siteName: (string?)result["sitename"],
					wikiId: (string?)result["wikiid"],
					apiUrl: (string?)result["api"]));
			}

			return retval;
		}

		private static List<SiteInfoLanguage>? GetLanguages(JToken parent)
		{
			if (!(parent["languages"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoLanguage>();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoLanguage(result.MustHaveString("code"), result.MustHaveBCString("name")));
			}

			return retval;
		}

		private static List<SiteInfoLibrary>? GetLibraries(JToken parent)
		{
			if (!(parent["libraries"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoLibrary>();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoLibrary(
					name: result.MustHaveString("name"),
					version: result.MustHaveString("version")));
			}

			return retval;
		}

		private static List<SiteInfoMagicWord>? GetMagicWords(JToken parent)
		{
			if (!(parent["magicwords"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoMagicWord>();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoMagicWord(
					name: result.MustHaveString("name"),
					aliases: result.MustHaveList<string>("aliases"),
					caseSensitive: result["case-sensitive"].ToBCBool()));
			}

			return retval;
		}

		private static List<SiteInfoNamespaceAlias>? GetNamespaceAliases(JToken parent)
		{
			if (!(parent["namespacealiases"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoNamespaceAlias>();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoNamespaceAlias(
					id: (int)result.MustHave("id"),
					alias: result.MustHaveBCString("alias")));
			}

			return retval;
		}

		private static List<SiteInfoNamespace>? GetNamespaces(JToken parent)
		{
			if (!(parent["namespaces"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoNamespace>();
			foreach (var resultNode in node)
			{
				if (resultNode.First is JToken result)
				{
					retval.Add(new SiteInfoNamespace(
						id: (int)result.MustHave("id"),
						canonicalName: (string?)result["canonical"] ?? string.Empty,
						defaultContentModel: (string?)result["defaultcontentmodel"],
						flags: ((string?)result["case"] == "case-sensitive" ? NamespaceFlags.CaseSensitive : NamespaceFlags.None) | result.GetFlags(
							("content", NamespaceFlags.ContentSpace),
							("nonincludable", NamespaceFlags.NonIncludable),
							("subpages", NamespaceFlags.Subpages)),
						name: result.MustHaveBCString("name")));
				}
			}

			return retval;
		}

		private static List<string>? GetProtocols(JToken parent) => parent["protocols"] is JToken node ? node.ToList<string>() : null;

		private static SiteInfoRestriction? GetRestrictions(JToken parent) => parent["restrictions"] is JToken node
			? new SiteInfoRestriction(
				cascadingLevels: node["cascadinglevels"].ToReadOnlyList<string>(),
				levels: node["levels"].ToReadOnlyList<string>(),
				semiProtectedLevels: node["semiprotectedlevels"].ToReadOnlyList<string>(),
				types: node["types"].ToReadOnlyList<string>())
			: null;

		private static (SiteInfoSkin? defaultSkin, List<SiteInfoSkin>? skins) GetSkins(JToken parent)
		{
			if (!(parent["skins"] is JToken node))
			{
				return (null, null);
			}

			SiteInfoSkin? defaultSkin = null;
			var retval = new List<SiteInfoSkin>();
			foreach (var result in node)
			{
				var item = new SiteInfoSkin(
					code: result.MustHaveString("code"),
					name: result.MustHaveBCString("name"),
					unusable: result["unusable"].ToBCBool());
				if (result["default"].ToBCBool())
				{
					defaultSkin = item;
				}

				retval.Add(item);
			}

			return (defaultSkin, retval);
		}

		private static List<SiteInfoSpecialPageAlias>? GetSpecialPageAliases(JToken parent)
		{
			if (!(parent["specialpagealiases"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoSpecialPageAlias>();
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

		private static List<SiteInfoSubscribedHook>? GetSubscribedHooks(JToken parent)
		{
			if (!(parent["showhooks"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoSubscribedHook>();
			foreach (var result in node)
			{
				// See https://phabricator.wikimedia.org/T117022 for details on the Scribunto check.
				var subscribersNode = result.MustHave("subscribers");
				var subscribers = subscribersNode.Type == JTokenType.Object && subscribersNode["scribunto"] is JToken scribuntoNode
					? new List<string> { (string?)scribuntoNode ?? string.Empty }
					: subscribersNode.ToList<string>();

				retval.Add(new SiteInfoSubscribedHook(
					name: result.MustHaveString("name"),
					subscribers: subscribers));
			}

			return retval;
		}

		private static List<SiteInfoUserGroup>? GetUserGroups(JToken parent)
		{
			if (!(parent["usergroups"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoUserGroup>();
			foreach (var result in node)
			{
				retval.Add(new SiteInfoUserGroup(
					name: result.MustHaveString("name"),
					rights: result.MustHaveList<string>("rights"),
					number: (long?)result["number"] ?? -1,
					add: result["add"].ToReadOnlyList<string>(),
					addSelf: result["add-self"].ToReadOnlyList<string>(),
					remove: result["remove"].ToReadOnlyList<string>(),
					removeSelf: result["remove-self"].ToReadOnlyList<string>()));
			}

			return retval;
		}

		private static List<string>? GetVariables(JToken parent) => parent["variables"] is JToken node ? node.ToList<string>() : null;
		#endregion

		#region Private Methods
		private List<SiteInfoExtensions>? GetExtensions(JToken parent)
		{
			if (!(parent["extensions"] is JToken node))
			{
				return null;
			}

			var retval = new List<SiteInfoExtensions>();
			foreach (var result in node)
			{
				var name = (string?)result["name"];
				IReadOnlyList<string>? descMsgParams = null;
				try
				{
					descMsgParams = result["descriptionmsgparams"].ToReadOnlyList<string>();
				}
				catch (InvalidCastException)
				{
					this.Wal.AddWarning("siteinfo-unhandledparams", CurrentCulture(EveMessages.UnhandledParams, name));
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
			if (!(parent["general"] is JToken node))
			{
				return null;
			}

			var fallback = new List<string>();
			var fallbacksNode = node["fallback"];
			if (fallbacksNode != null)
			{
				foreach (var token in fallbacksNode)
				{
					fallback.Add(token.MustHaveString("code"));
				}
			}

			var imageLimits = new Dictionary<string, ImageLimitsItem>();
			if (node["imagelimits"] is JToken imageLimitsNode)
			{
				foreach (var token in imageLimitsNode.ToBCIndexedList(this.Wal.DetectedFormatVersion))
				{
					var value = token.value;
					imageLimits.Add(token.key, new ImageLimitsItem((int)value.MustHave("width"), (int)value.MustHave("height")));
				}
			}

			var server = node.MustHaveString("server");
			var serverName = (string?)node["servername"];
			if (serverName == null)
			{
				// Same approach as MediaWiki uses in Setup.php
				var canonical = server.StartsWith("//", StringComparison.Ordinal) ? "http:" + server : server;
				var uri = new Uri(canonical);
				serverName = uri.Host;
			}

			var thumbLimits = new Dictionary<string, int>();
			if (node["thumblimits"] is JToken thumbLimitsNode)
			{
				foreach (var (key, value) in thumbLimitsNode.ToBCIndexedList(this.Wal.DetectedFormatVersion))
				{
					thumbLimits.Add(key, (int)value);
				}
			}

			var timeOffsetNode = (double)node.MustHave("timeoffset");
			var timeOffset = TimeSpan.FromMinutes(timeOffsetNode);

			// Default value is "false", which gets emitted in JSON, so check for that.
			var variantArticlePathNode = node.MustHave("variantarticlepath");
			var variantArticlePath = variantArticlePathNode?.Type == JTokenType.Boolean ? null : (string?)variantArticlePathNode;

			var variants = new List<string>();
			if (node["variants"] is JToken variantsNode)
			{
				foreach (var token in variantsNode)
				{
					variants.Add(token.MustHaveString("code"));
				}
			}

			return new SiteInfoGeneral(
				articlePath: node.MustHaveString("articlepath"),
				basePage: node.MustHaveString("base"),
				dbType: node.MustHaveString("dbtype"),
				dbVersion: node.MustHaveString("dbversion"),
				externalImages: node["externalimages"].ToReadOnlyList<string>(),
				fallback8BitEncoding: node.MustHaveString("fallback8bitEncoding"),
				fallbackLanguages: fallback,
				favicon: (string?)node["favicon"],
				flags: ((string?)node["case"] == "case-sensitive" ? SiteInfoFlags.CaseSensitive : SiteInfoFlags.None) | node.GetFlags(
					("imagewhitelistenabled", SiteInfoFlags.ImageWhitelistEnabled),
					("langconversion", SiteInfoFlags.LanguageConversion),
					("misermode", SiteInfoFlags.MiserMode),
					("readonly", SiteInfoFlags.ReadOnly),
					("righttoleft", SiteInfoFlags.RightToLeft),
					("titleconversion", SiteInfoFlags.TitleConversion),
					("writeapi", SiteInfoFlags.WriteApi)),
				generator: node.MustHaveString("generator"),
				gitBranch: (string?)node["git-branch"],
				gitHash: (string?)node["git-hash"],
				hhvmVersion: (string?)node["hhvmversion"],
				imageLimits: imageLimits,
				language: node.MustHaveString("lang"),
				legalTitleChars: (string?)node["legaltitlechars"],
				linkPrefix: (string?)node["linkprefix"],
				linkPrefixCharset: (string?)node["linkprefixcharset"],
				linkTrail: (string?)node["linktrail"],
				logo: (string?)node["logo"],
				mainPage: node.MustHaveString("mainpage"),
				maxUploadSize: (long?)node["maxuploadsize"] ?? 0,
				phpSapi: node.MustHaveString("phpsapi"),
				phpVersion: node.MustHaveString("phpversion"),
				readOnlyReason: (string?)node["readonlyreason"],
				revision: (long?)node["revision"] ?? 0,
				script: node.MustHaveString("script"),
				scriptPath: node.MustHaveString("scriptpath"),
				server: server,
				serverName: serverName,
				siteName: node.MustHaveString("sitename"),
				thumbLimits: thumbLimits,
				time: node.MustHaveDate("time"),
				timeOffset: timeOffset,
				timeZone: node.MustHaveString("timezone"),
				variantArticlePath: variantArticlePath,
				variants: variants,
				wikiId: node.MustHaveString("wikiid"));
		}

		private SiteInfoRights? GetRightsInfo(JToken parent) => parent["rightsinfo"] is JToken node ? new SiteInfoRights((string?)node["text"], (string?)node["url"]) : null;
		#endregion
	}
}