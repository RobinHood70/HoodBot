#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WallE.Properties.EveMessages;
	using static RobinHood70.WikiCommon.Globals;

	internal class MetaSiteInfo : QueryModule<SiteInfoInput, SiteInfoResult>
	{
		#region Constructors
		public MetaSiteInfo(WikiAbstractionLayer wal, SiteInfoInput input)
			: base(wal, input, new SiteInfoResult(), null)
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
			if (this.SiteVersion == 0 && from == "main" && text?.Contains("formatversion") == true)
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

		protected override void DeserializeResult(JToken result, SiteInfoResult output)
		{
		}

		protected override void DeserializeParent(JToken parent, SiteInfoResult output)
		{
			// Because this module can continue in non-standard fashion (each module will either appear in whole or not at all), we need to ensure that outputs are only written to if necessary.
			GetGeneral(parent, output, this.Wal);
			GetNamespaces(parent, output);
			GetNamespaceAliases(parent, output);
			GetSpecialPageAliases(parent, output);
			GetMagicWords(parent, output);
			GetInterwikiMap(parent, output);
			GetDbReplLag(parent, output);
			GetStatistics(parent, output);
			GetUserGroups(parent, output);
			GetFileExtensions(parent, output);
			GetLibraries(parent, output);
			GetExtensions(parent, output, this.Wal);
			GetRightsInfo(parent, output);
			GetRestrictions(parent, output);
			GetLanguages(parent, output);
			GetSkins(parent, output);
			GetExtensionTags(parent, output);
			GetFunctionHooks(parent, output);
			GetVariables(parent, output);
			GetProtocols(parent, output);
			GetDefaultOptions(parent, output);
			GetSubscribedHooks(parent, output);
		}
		#endregion

		#region Private Static Methods
		private static void GetDbReplLag(JToken parent, SiteInfoResult output)
		{
			var node = parent["dbrepllag"];
			if (node != null)
			{
				var outputList = new List<LagItem>();
				foreach (var result in node)
				{
					var item = new LagItem()
					{
						Host = (string)result["host"],
						Lag = (int)result["lag"],
					};
					outputList.Add(item);
				}

				output.LagInfo = outputList;
			}
		}

		private static void GetDefaultOptions(JToken parent, SiteInfoResult output)
		{
			var node = parent["defaultoptions"];
			if (node != null)
			{
				output.DefaultOptions = node.AsReadOnlyDictionary<string, object>();
			}
		}

		private static void GetExtensions(JToken parent, SiteInfoResult output, WikiAbstractionLayer wal)
		{
			var node = parent["extensions"];
			if (node != null)
			{
				var outputList = new List<ExtensionItem>();
				foreach (var result in node)
				{
					var item = new ExtensionItem()
					{
						Type = (string)result["type"],
						Name = (string)result["name"],
						NameMessage = (string)result["namemsg"],
						Description = (string)result["description"],
						DescriptionMessage = (string)result["descriptionmsg"],
					};
					try
					{
						item.DescriptionMessageParameters = result["descriptionmsgparams"].AsReadOnlyList<string>();
					}
					catch (InvalidCastException)
					{
						wal.AddWarning("siteinfo-unhandledparams", CurrentCulture(UnhandledParams, item.Name));
					}

					item.Author = (string)result["author"];
					item.Url = (string)result["url"];
					item.Version = (string)result["version"];
					item.VersionControlSystem = (string)result["vcs-system"];
					item.VersionControlSystemVersion = (string)result["vcs-version"];
					item.VersionControlSystemUrl = (string)result["vcs-url"];
					item.VersionControlSystemDate = (DateTime?)result["vcs-date"];
					item.LicenseName = (string)result["license-name"];
					item.License = (string)result["license"];
					item.Credits = (string)result["credits"];

					outputList.Add(item);
				}

				output.Extensions = outputList;
			}
		}

		private static void GetExtensionTags(JToken parent, SiteInfoResult output)
		{
			var node = parent["extensiontags"];
			if (node != null)
			{
				output.ExtensionTags = node.AsReadOnlyList<string>();
			}
		}

		private static void GetFileExtensions(JToken parent, SiteInfoResult output)
		{
			var node = parent["fileextensions"];
			if (node != null)
			{
				var outputList = new List<string>();
				foreach (var result in node)
				{
					outputList.Add((string)result["ext"]);
				}

				output.FileExtensions = outputList;
			}
		}

		private static void GetFunctionHooks(JToken parent, SiteInfoResult output)
		{
			var node = parent["functionhooks"];
			if (node != null)
			{
				output.FunctionHooks = node.AsReadOnlyList<string>();
			}
		}

		private static void GetGeneral(JToken parent, SiteInfoResult output, WikiAbstractionLayer wal)
		{
			var result = parent["general"];
			if (result != null)
			{
				output.ArticlePath = (string)result["articlepath"];
				output.BasePage = (string)result["base"];
				output.DbType = (string)result["dbtype"];
				output.DbVersion = (string)result["dbversion"];
				output.ExternalImages = result["externalimages"].AsReadOnlyList<string>();
				output.Fallback8BitEncoding = (string)result["fallback8bitEncoding"];

				var fallback = new List<string>();
				if (result["fallback"] != null)
				{
					foreach (var token in result["fallback"])
					{
						fallback.Add((string)token["code"]);
					}
				}

				output.FallbackLanguages = fallback;
				output.Favicon = (string)result["favicon"];
				output.Flags =
					((string)result["case"] == "case-sensitive" ? SiteInfoFlags.CaseSensitive : SiteInfoFlags.None) |
					result.GetFlag("imagewhitelistenabled", SiteInfoFlags.ImageWhitelistEnabled) |
					result.GetFlag("langconversion", SiteInfoFlags.LanguageConversion) |
					result.GetFlag("misermode", SiteInfoFlags.MiserMode) |
					result.GetFlag("readonly", SiteInfoFlags.ReadOnly) |
					result.GetFlag("righttoleft", SiteInfoFlags.RightToLeft) |
					result.GetFlag("titleconversion", SiteInfoFlags.TitleConversion) |
					result.GetFlag("writeapi", SiteInfoFlags.WriteApi);
				output.Generator = (string)result["generator"];
				output.GitBranch = (string)result["git-branch"];
				output.GitHash = (string)result["git-hash"];
				output.HhvmVersion = (string)result["hhvmversion"];

				var imageLimits = new Dictionary<string, ImageLimitsItem>();
				if (result["imagelimits"] != null)
				{
					var counter = 0;
					foreach (var token in result["imagelimits"])
					{
						JToken actualToken;
						string key;
						if (wal.DetectedFormatVersion == 2)
						{
							var prop = token as JProperty;
							key = prop.Name;
							actualToken = prop.Value;
						}
						else
						{
							key = counter.ToStringInvariant();
							counter++;
							actualToken = token;
						}

						var width = (int)actualToken["width"];
						var height = (int)actualToken["height"];
						imageLimits.Add(key, new ImageLimitsItem(width, height));
					}
				}

				output.ImageLimits = imageLimits;
				output.Language = (string)result["lang"];
				output.LegalTitleChars = (string)result["legaltitlechars"];
				output.LinkPrefix = (string)result["linkprefix"];
				output.LinkPrefixCharset = (string)result["linkprefixcharset"];
				output.LinkTrail = (string)result["linktrail"];
				output.Logo = (string)result["logo"];
				output.MainPage = (string)result["mainpage"];
				output.MaxUploadSize = (long?)result["maxuploadsize"] ?? 0;
				output.PhpSapi = (string)result["phpsapi"];
				output.PhpVersion = (string)result["phpversion"];
				output.ReadOnlyReason = (string)result["readonlyreason"];
				output.Revision = (long?)result["revision"] ?? 0;
				output.RightsText = (string)result["rightstext"];
				output.Script = (string)result["script"];
				output.ScriptPath = (string)result["scriptpath"];
				output.Server = (string)result["server"];
				output.ServerName = (string)result["servername"];
				if (output.ServerName == null)
				{
					// Same basic approach as MediaWiki uses
					var canonical = output.Server.StartsWith("//", StringComparison.Ordinal) ? "http:" + output.Server : output.Server;
					var uri = new Uri(canonical);
					output.ServerName = uri.Host;
				}

				output.SiteName = (string)result["sitename"];

				var thumbLimits = result["thumblimits"];
				if (thumbLimits != null)
				{
					if (wal.DetectedFormatVersion == 2)
					{
						output.ThumbLimits = thumbLimits.AsReadOnlyDictionary<string, int>();
					}
					else
					{
						var dict = new Dictionary<string, int>();
						var counter = 0;
						foreach (var limit in thumbLimits)
						{
							dict.Add(counter.ToStringInvariant(), (int)limit);
							counter++;
						}

						output.ThumbLimits = dict;
					}
				}

				output.Time = (DateTime?)result["time"];

				var timeOffset = (int?)result["timeoffset"];
				output.TimeOffset = timeOffset == null ? (TimeSpan?)null : TimeSpan.FromMinutes(timeOffset.Value);
				output.TimeZone = (string)result["timezone"];

				// Default value is "false", which gets emitted in JSON, so check for that.
				var variantArticlePath = result["variantarticlepath"];
				if (variantArticlePath.Type != JTokenType.Boolean)
				{
					output.VariantArticlePath = (string)variantArticlePath;
				}

				var variants = new List<string>();
				if (result["variants"] != null)
				{
					foreach (var token in result["variants"])
					{
						variants.Add((string)token["code"]);
					}
				}

				output.Variants = variants;
				output.WikiId = (string)result["wikiid"];
			}
		}

		private static void GetInterwikiMap(JToken parent, SiteInfoResult output)
		{
			var node = parent["interwikimap"];
			if (node != null)
			{
				var outputList = new List<InterwikiMapItem>();
				foreach (var result in node)
				{
					var item = new InterwikiMapItem()
					{
						Prefix = (string)result["prefix"],
						Flags =
						result.GetFlag("extralanguagelink", InterwikiMapFlags.ExtraLanguageLink) |
						result.GetFlag("local", InterwikiMapFlags.Local) |
						result.GetFlag("localinterwiki", InterwikiMapFlags.LocalInterwiki) |
						result.GetFlag("protorel", InterwikiMapFlags.ProtocolRelative) |
						result.GetFlag("trans", InterwikiMapFlags.TransclusionAllowed),
						Language = (string)result["language"],
						LinkText = (string)result["linktext"],
						SiteName = (string)result["sitename"],
						Url = (string)result["url"],
						WikiId = (string)result["wikiid"],
						ApiUrl = (string)result["api"],
					};
					outputList.Add(item);
				}

				output.InterwikiMap = outputList;
			}
		}

		private static void GetLanguages(JToken parent, SiteInfoResult output)
		{
			var node = parent["languages"];
			if (node != null)
			{
				var outputList = new List<LanguageItem>();
				foreach (var result in node)
				{
					var item = new LanguageItem()
					{
						Code = (string)result["code"],
						Name = (string)result.AsBCContent("name"),
					};
					outputList.Add(item);
				}

				output.Languages = outputList;
			}
		}

		private static void GetLibraries(JToken parent, SiteInfoResult output)
		{
			var node = parent["libraries"];
			if (node != null)
			{
				var outputList = new List<LibrariesItem>();
				foreach (var result in node)
				{
					var item = new LibrariesItem()
					{
						Name = (string)result["name"],
						Version = (string)result["version"],
					};
					outputList.Add(item);
				}

				output.Libraries = outputList;
			}
		}

		private static void GetMagicWords(JToken parent, SiteInfoResult output)
		{
			var node = parent["magicwords"];
			if (node != null)
			{
				var outputList = new List<MagicWordsItem>();
				foreach (var result in node)
				{
					var item = new MagicWordsItem()
					{
						Name = (string)result["name"],
						Aliases = result["aliases"].AsReadOnlyList<string>(),
						CaseSensitive = result["case-sensitive"].AsBCBool(),
					};
					outputList.Add(item);
				}

				output.MagicWords = outputList;
			}
		}

		private static void GetNamespaceAliases(JToken parent, SiteInfoResult output)
		{
			var node = parent["namespacealiases"];
			if (node != null)
			{
				var outputList = new List<NamespaceAliasesItem>();
				foreach (var result in node)
				{
					var item = new NamespaceAliasesItem()
					{
						Id = (int)result["id"],
						Alias = (string)result.AsBCContent("alias"),
					};
					outputList.Add(item);
				}

				output.NamespaceAliases = outputList;
			}
		}

		private static void GetNamespaces(JToken parent, SiteInfoResult output)
		{
			var node = parent["namespaces"];
			if (node != null)
			{
				var outputList = new List<NamespacesItem>();
				foreach (var resultNode in node)
				{
					var result = resultNode.First;
					var item = new NamespacesItem()
					{
						CanonicalName = (string)result["canonical"] ?? string.Empty,
						DefaultContentModel = (string)result["defaultcontentmodel"],
						Flags =
						((string)result["case"] == "case-sensitive" ? NamespaceFlags.CaseSensitive : NamespaceFlags.None) |
						result.GetFlag("content", NamespaceFlags.ContentSpace) |
						result.GetFlag("nonincludable", NamespaceFlags.NonIncludable) |
						result.GetFlag("subpages", NamespaceFlags.Subpages),
						Id = (int)result["id"],
						Name = (string)result.AsBCContent("name"),
					};
					outputList.Add(item);
				}

				output.Namespaces = outputList;
			}
		}

		private static void GetProtocols(JToken parent, SiteInfoResult output)
		{
			var node = parent["protocols"];
			if (node != null)
			{
				output.Protocols = node.AsReadOnlyList<string>();
			}
		}

		private static void GetRestrictions(JToken parent, SiteInfoResult output)
		{
			var node = parent["restrictions"];
			if (node != null)
			{
				var item = new RestrictionsItem()
				{
					CascadingLevels = node["cascadinglevels"].AsReadOnlyList<string>(),
					Levels = node["levels"].AsReadOnlyList<string>(),
					SemiProtectedLevels = node["semiprotectedlevels"].AsReadOnlyList<string>(),
					Types = node["types"].AsReadOnlyList<string>(),
				};
				output.Restrictions = item;
			}
		}

		private static void GetRightsInfo(JToken parent, SiteInfoResult output)
		{
			var result = parent["rightsinfo"];
			if (result != null)
			{
				output.RightsText = (string)result["text"]; // Overwrites the version coming from the General branch if both are present. This is probably preferred, since this one gets $wgRightsPage if $wgRightsText isn't present.
				output.RightsUrl = (string)result["url"];
			}
		}

		private static void GetSubscribedHooks(JToken parent, SiteInfoResult output)
		{
			var node = parent["showhooks"];
			if (node != null)
			{
				var outputList = new List<SubscribedHooksItem>();
				foreach (var result in node)
				{
					var item = new SubscribedHooksItem()
					{
						Name = (string)result["name"],
					};
					var subscribers = result["subscribers"];
					if (subscribers.Type == JTokenType.Array)
					{
						item.Subscribers = result["subscribers"].AsReadOnlyList<string>();
					}
					else if (subscribers.Type == JTokenType.Object && subscribers["scribunto"] != null)
					{
						// Compensates for Scribunto oddness noted at https://phabricator.wikimedia.org/T117022
						var list = new List<string>
						{
							(string)subscribers["scribunto"],
						};
						item.Subscribers = list;
					}

					outputList.Add(item);
				}

				output.SubscribedHooks = outputList;
			}
		}

		private static void GetSkins(JToken parent, SiteInfoResult output)
		{
			var node = parent["skins"];
			if (node != null)
			{
				var outputList = new List<SkinsItem>();
				foreach (var result in node)
				{
					var item = new SkinsItem()
					{
						Code = (string)result["code"],
						Name = (string)result.AsBCContent("name"),
						Unusable = result["unusable"].AsBCBool(),
					};
					if (result["default"].AsBCBool())
					{
						output.DefaultSkin = item;
					}

					outputList.Add(item);
				}

				output.Skins = outputList;
			}
		}

		private static void GetSpecialPageAliases(JToken parent, SiteInfoResult output)
		{
			var node = parent["specialpagealiases"];
			if (node != null)
			{
				var outputList = new List<SpecialPageAliasesItem>();
				foreach (var result in node)
				{
					var item = new SpecialPageAliasesItem()
					{
						RealName = (string)result["realname"],
						Aliases = result["aliases"].AsReadOnlyList<string>(),
					};
					outputList.Add(item);
				}

				output.SpecialPageAliases = outputList;
			}
		}

		private static void GetStatistics(JToken parent, SiteInfoResult output)
		{
			var result = parent["statistics"];
			if (result != null)
			{
				var item = new StatisticsInfo()
				{
					ActiveUsers = (long?)result["activeusers"] ?? 0,
					Admins = (long?)result["admins"] ?? 0,
					Articles = (long?)result["articles"] ?? 0,
					Edits = (long?)result["edits"] ?? 0,
					Images = (long?)result["images"] ?? 0,
					Jobs = (long?)result["jobs"] ?? 0,
					Pages = (long?)result["pages"] ?? 0,
					Users = (long?)result["users"] ?? 0,
					Views = (long?)result["views"] ?? 0,
				};
				output.Statistics = item;
			}
		}

		private static void GetUserGroups(JToken parent, SiteInfoResult output)
		{
			var node = parent["usergroups"];
			if (node != null)
			{
				var outputList = new List<UserGroupsItem>();
				foreach (var result in node)
				{
					var item = new UserGroupsItem()
					{
						Name = (string)result["name"],
						Number = (long?)result["number"] ?? -1,
						Rights = result["rights"].AsReadOnlyList<string>(),
						Add = result["add"].AsReadOnlyList<string>(),
						AddSelf = result["add-self"].AsReadOnlyList<string>(),
						Remove = result["remove"].AsReadOnlyList<string>(),
						RemoveSelf = result["remove-self"].AsReadOnlyList<string>(),
					};
					outputList.Add(item);
				}

				output.UserGroups = outputList;
			}
		}

		private static void GetVariables(JToken parent, SiteInfoResult output)
		{
			var node = parent["variables"];
			if (node != null)
			{
				output.Variables = node.AsReadOnlyList<string>();
			}
		}
		#endregion
	}
}