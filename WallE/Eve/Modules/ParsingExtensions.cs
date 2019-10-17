#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Runtime.CompilerServices;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.ProjectGlobals;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>Any other API-related items that didn't warrant creation of their own static class.</summary>
	public static class ParsingExtensions
	{
		#region Private Constants
		private const string Unknown = "<Unknown>";
		#endregion

		#region JToken Methods
		public static void AddPropertiesToDictionary(this JToken result, IDictionary<string, string?> dict)
		{
			foreach (var node in result.Children<JProperty>())
			{
				dict.Add(node.Name, (string?)node.Value);
			}
		}

		// This routine makes the assumption that the value will always be one of { null, "", boolean }, since these are the only values that MediaWiki should ever emit, therefore no error checking is done.
		public static bool AsBCBool(this JToken? value) =>
			value == null ? false :
			value.Type == JTokenType.String ? true :
			(bool)value;

		public static string AsBCString(this JToken token, string name, [CallerMemberName]string caller = Unknown)
		{
			ThrowNull(token, nameof(token));
			var node = token[name] ?? token["*"] ?? throw MalformedException(token, name, caller);
			return (string?)node ?? throw MalformedException(token, name, caller);
		}

		public static string? AsBCStringOptional(this JToken token, string name)
		{
			ThrowNull(token, nameof(token));
			return (string?)(token[name] ?? token["*"]);
		}

		public static ReadOnlyDictionary<string, string?> AsBCDictionary(this JToken? token)
		{
			var dict = new Dictionary<string, string?>();
			if (token != null)
			{
				if (token.Type == JTokenType.Array)
				{
					foreach (var entry in token)
					{
						var key = (string)entry["name"]!;
						var value = (string?)entry["*"];

						dict.Add(key, value);
					}
				}
				else
				{
					token.AddPropertiesToDictionary(dict);
				}
			}

			return dict.AsReadOnly();
		}

		public static JToken? AsBCSubContent(this JToken token) => token?.Type == JTokenType.Object ? token?["*"] : token;

		public static DateTime AsDateNotNull(this JToken token, string name, [CallerMemberName] string caller = Unknown)
		{
			ThrowNull(token, nameof(token));
			var node = token[name] ?? throw MalformedException(token, name, caller);
			return AsDate((string?)node) ?? throw MalformedException(token, name, caller);
		}

		public static DateTime? AsDate(this JToken? date) => date == null ? null : AsDate((string?)date);

		public static IReadOnlyList<T> AsReadOnlyList<T>(this JToken? token) => token?.Values<T>().AsReadOnlyList() ?? Array.Empty<T>();

		public static IReadOnlyDictionary<string, TValue> AsReadOnlyDictionary<TValue>(this JToken? token)
		{
			if (token == null)
			{
				return ImmutableDictionary<string, TValue>.Empty;
			}

			// Internally, ToObject gets rather convoluted when dealing with complex types. Since we're always dealing with strings at the first type anyway, I've changed token.ToObject<Dictionary<TKey, TValue>>() to only call ToObject() on the value, which should normally be a simple type.
			var dict = new Dictionary<string, TValue>();
			foreach (var item in token.Children<JProperty>())
			{
				dict.Add(item.Name, item.Value.ToObject<TValue>());
			}

			return new ReadOnlyDictionary<string, TValue>(dict);
		}

		public static ErrorItem GetError(this JToken result) => GetError(result, "code", "info");

		public static ErrorItem GetError(this JToken result, string codeName, string infoName) =>
			new ErrorItem(result.SafeString(codeName), result.SafeString(infoName));

		public static T GetFlag<T>(this JToken result, string nodeName, T value) => result[nodeName].AsBCBool() ? value : default;

		public static List<InterwikiTitleItem> GetInterwikiLinks(this JToken? result)
		{
			var output = new List<InterwikiTitleItem>();
			if (result != null)
			{
				foreach (var item in result)
				{
					output.Add(new InterwikiTitleItem(item.SafeString("iw"), item.SafeString("title"), (Uri?)item["url"]));
				}
			}

			return output;
		}

		public static LanguageLinksItem? GetLanguageLink(this JToken? result) => result == null
			? null
			: new LanguageLinksItem()
			{
				Language = (string?)result["lang"],
				Url = (Uri)result["url"],
				Name = (string?)result["langname"],
				Autonym = (string?)result["autonym"],
				Title = (string?)result["title"],
			};

		public static void GetRedirects(this JToken? result, Dictionary<string, PageSetRedirectItem> redirects, WikiAbstractionLayer wal)
		{
			const string fromName = "from";
			const string toName = "to";
			const string toFragmentName = "tofragment";
			const string toInterwikiName = "tointerwiki";

			if (result != null)
			{
				foreach (var item in result)
				{
					var to = item.SafeString(toName);
					var interwiki = (string?)item[toInterwikiName];
					if (interwiki == null && wal.SiteVersion < 125)
					{
						// Pre-1.25 code did not split out interwiki prefixes from the title, so do that.
						var titleSplit = to.TrimStart(TextArrays.Colon).Split(TextArrays.Colon, 2);
						if (titleSplit.Length == 2 && wal.InterwikiPrefixes.Contains(titleSplit[0]))
						{
							interwiki = titleSplit[0];
							to = titleSplit[1];
						}
					}

					var gi = item.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
					gi.Remove(fromName);
					gi.Remove(toName);
					gi.Remove(toFragmentName);
					gi.Remove(toInterwikiName);

					var toPage = new PageSetRedirectItem(
						title: to,
						fragment: (string?)item[toFragmentName],
						interwiki: interwiki,
						generatorInfo: gi);
					redirects[item.SafeString(fromName)] = toPage;
				}
			}
		}

		public static RevisionsItem GetRevision(this JToken result, string pageTitle)
		{
			ThrowNull(result, nameof(result));
			var revision = new RevisionsItem()
			{
				Comment = (string?)result["comment"],
				Content = result.AsBCStringOptional("content"),
				ContentFormat = (string?)result["contentformat"],
				ContentModel = (string?)result["contentmodel"],
				Flags =
					GetFlag(result, "anon", RevisionFlags.Anonymous) |
					GetFlag(result, "commenthidden", RevisionFlags.CommentHidden) |
					GetFlag(result, "minor", RevisionFlags.Minor) |
					GetFlag(result, "sha1hidden", RevisionFlags.Sha1Hidden) |
					GetFlag(result, "suppressed", RevisionFlags.Suppressed) |
					GetFlag(result, "texthidden", RevisionFlags.TextHidden) |
					GetFlag(result, "userhidden", RevisionFlags.UserHidden),
				ParentId = (long?)result["parentid"] ?? 0,
				ParsedComment = (string?)result["parsedcomment"],
				ParseTree = (string?)result["parsetree"],
				RevisionId = (long?)result["revid"] ?? 0,
				RollbackToken = (string?)result["rollbacktoken"],
				Sha1 = (string?)result["sha1"],
				Size = (long?)result["size"] ?? 0,
				Tags = result["tags"].AsReadOnlyList<string>(),
				Timestamp = (DateTime?)result["timestamp"],
				User = (string?)result["user"],
				UserId = (long?)result["userid"] ?? -1,
			};

			if (revision.Sha1 != null)
			{
				if (revision.Sha1.TrimStart('0').Length == 0)
				{
					// If it's all zeroes, switch it to null and ignore it. This is caused by SHA-1 values beginning with either 0x or 0b, as documented here: https://bugs.php.net/bug.php?id=50175 and https://bugs.php.net/bug.php?id=55398.
					revision.Sha1 = null;
				}
				else if (revision.Content != null && revision.Content.GetHash(HashType.Sha1) != revision.Sha1)
				{
					// CONSIDER: This was changed from a warning to an exception. Should it be handled in Eve or allowed to fall through to the caller?
					throw new ChecksumException(CurrentCulture(EveMessages.RevisionSha1Failed, revision.RevisionId, pageTitle));
				}
			}

			return revision;
		}

		public static List<RevisionsItem> GetRevisions(this JToken result, string title)
		{
			var revisions = new List<RevisionsItem>();
			var resultNode = result["revisions"];
			if (resultNode != null)
			{
				foreach (var revisionNode in resultNode)
				{
					revisions.Add(GetRevision(revisionNode, title));
				}
			}

			return revisions;
		}

		public static T GetUserData<T>(this T user, JToken result)
			where T : InternalUserItem
		{
			// ListAllUsers returns an empty string, ListUsers returns null, so check for both.
			var regDate = (string?)result["registration"];
			var rights = result["rights"];
			if (rights != null)
			{
				if (rights.Type == JTokenType.Array)
				{
					user.Rights = rights.AsReadOnlyList<string>();
				}
				else
				{
					// Somewhere prior to 1.22, rights lists could be returned as a numbered key-value pair instead of a straight-forward string array, so this handles that situation and converts it into the expected type.
					var dict = rights.ToObject<Dictionary<int, string>>();
					if (dict != null)
					{
						user.Rights = dict.Values.AsReadOnlyList();
					}
				}
			}

			user.BlockId = (long?)result["blockid"] ?? 0;
			user.BlockedBy = (string?)result["blockedby"];
			user.BlockedById = (long?)result["blockedbyid"] ?? 0;
			user.BlockTimestamp = (DateTime?)result["blockedtimestamp"];
			user.BlockReason = (string?)result["blockreason"];
			user.BlockExpiry = result["blockexpiry"].AsDate();
			user.BlockHidden = result["hidden"].AsBCBool();
			user.EditCount = (long?)result["editcount"] ?? 0;
			user.Groups = result["groups"].AsReadOnlyList<string>();
			user.ImplicitGroups = result["implicitgroups"].AsReadOnlyList<string>();
			user.Registration = string.IsNullOrEmpty(regDate) ? null : (DateTime?)result["registration"];

			return user;
		}

		public static IReadOnlyList<WarningsItem> GetWarnings(this JToken? result)
		{
			var list = new List<WarningsItem>();
			if (result != null)
			{
				foreach (var item in result)
				{
					var warning = new WarningsItem((string)item["type"]!, (string?)item["message"], (item["params"]?.ToObject<IEnumerable<object>>()).AsReadOnlyList());
					list.Add(warning);
				}
			}

			return list.AsReadOnly();
		}

		public static WikiTitleItem GetWikiTitle(this JToken result) => new WikiTitleItem((int)result.NotNull("ns"), result.SafeString("title"), (long?)result["pageid"] ?? 0);

		public static JToken NotNull(this JToken token, string name, [CallerMemberName] string caller = Unknown) => token[name] ?? throw MalformedException(token, name, caller);

		public static string SafeString(this JToken token, string name, [CallerMemberName] string caller = Unknown)
		{
			ThrowNull(token, nameof(token));
			var node = token[name] ?? throw MalformedException(token, name, caller);
			return (string?)node ?? throw MalformedException(token, name, caller);
		}
		#endregion

		#region String Methods

		/// <summary>Figures out what kind of date we're dealing with (ISO 8601, all digits, or infinite and its variants) and then return the appropriate value.</summary>
		/// <param name="date">A string with the date.</param>
		/// <returns>A DateTime value or null. If the date evaluates to infinity, DateTime.MaxDate.</returns>
		public static DateTime? AsDate(this string? date)
		{
			switch (date)
			{
				case null:
				case "":
				case "indefinite":
				case "infinite":
				case "infinity":
				case "never":
					return null;
				default:
					return date.EndsWith("Z", StringComparison.Ordinal)
						? DateTime.Parse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
						: DateTime.ParseExact(date, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
			}
		}
		#endregion

		#region Request Methods
		public static Request BuildRevisions(this Request request, IRevisionsInput input, int siteVersion)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlags("prop", input.Properties)
				.Add("expandtemplates", input.ExpandTemplates)
				.Add("generatexml", input.GenerateXml)
				.Add("parse", input.Parse)
				.AddIf("section", input.Section, input.Section >= 0)
				.AddIfNotNull("diffto", MediaWikiGlobal.GetDiffToValue(input.DiffTo))
				.AddIfNotNull("difftotext", input.DiffToText)
				.AddIf("difftotextpst", input.DiffToTextPreSaveTransform, siteVersion >= 127)
				.AddIfNotNull("contentformat", input.ContentFormat)
				.Add("start", input.Start)
				.Add("end", input.End)
				.AddIf("dir", "newer", input.SortAscending)
				.AddIfNotNull(input.ExcludeUser ? "excludeuser" : "user", input.User);

			return request;
		}
		#endregion

		#region Private Methods
		private static WikiException MalformedException(JToken token, string name, string caller) => new WikiException(CurrentCulture(EveMessages.MalformedData, name, token.Path, caller));
		#endregion
	}
}