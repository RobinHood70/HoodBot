#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
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
	internal static class ParsingExtensions
	{
		#region General Methods

		// These methods are not extensions, but are placed in this class as useful but not warranting a class of their own yet.
		public static WikiException MalformedException(string name, JToken? token, [CallerMemberName]string caller = Unknown) => new WikiException(CurrentCulture(EveMessages.MalformedData, name, token?.Path, caller));

		// These methods are not extensions, but are placed in this class as useful but not warranting a class of their own yet.
		public static WikiException MalformedTypeException(string typeName, JToken? token, [CallerMemberName]string caller = Unknown) => new WikiException(CurrentCulture(EveMessages.MalformedDataType, typeName, token?.Path, caller));
		#endregion

		#region JToken Methods

		// Name reflects MediaWiki's ApiResult::META_BC_SUBELEMENTS.
		public static JToken? FromBCSubElements(this JToken? token) => token != null && token.Type == JTokenType.Object ? token["*"] : token;

		public static ErrorItem? GetError(this JToken? result) => GetError(result, "code", "info");

		public static ErrorItem? GetError(this JToken? result, string codeName, string infoName) => result == null
			? null
			: new ErrorItem(result.MustHaveString(codeName), result.MustHaveString(infoName));

		public static List<ErrorItem> GetErrors(this JToken? result)
		{
			var list = new List<ErrorItem>();
			if (result != null)
			{
				if (result["errors"] is JToken errors)
				{
					foreach (var node in errors)
					{
						if (node.GetError() is ErrorItem error)
						{
							list.Add(error);
						}
					}
				}
			}

			return list;
		}

		public static T GetFlags<T>(this JToken token, params (string key, T value)[] nodeNames)
		where T : Enum
		{
			ulong retval = 0;
			if (token != null && nodeNames != null)
			{
				foreach (var (key, value) in nodeNames)
				{
					retval |= Convert.ToUInt64(token[key].ToBCBool() ? value : default);
				}
			}

			return (T)Enum.ToObject(typeof(T), retval);
		}

		public static List<InterwikiTitleItem> GetInterwikiLinks(this JToken? token)
		{
			var output = new List<InterwikiTitleItem>();
			if (token != null)
			{
				foreach (var item in token)
				{
					output.Add(new InterwikiTitleItem(item.MustHaveString("iw"), item.MustHaveString("title"), (Uri?)item["url"]));
				}
			}

			return output;
		}

		public static LanguageLinksItem GetLanguageLink(this JToken token) => new LanguageLinksItem(
			language: token.MustHaveString("lang"),
			title: token.MustHaveString("title"),
			autonym: (string?)token["autonym"],
			name: (string?)token["langname"],
			url: (Uri?)token["url"]);

		public static Dictionary<string, PageSetRedirectItem> GetRedirects(this JToken? token, IReadOnlyCollection<string> interwikiPrefixes, int siteVersion)
		{
			const string fromName = "from";
			const string toName = "to";
			const string toFragmentName = "tofragment";
			const string toInterwikiName = "tointerwiki";

			ThrowNull(interwikiPrefixes, nameof(interwikiPrefixes));
			var redirects = new Dictionary<string, PageSetRedirectItem>();
			if (token != null)
			{
				foreach (var item in token)
				{
					var to = item.MustHaveString(toName);
					var interwiki = (string?)item[toInterwikiName];
					if (interwiki == null && siteVersion < 125)
					{
						// Pre-1.25 code did not split out interwiki prefixes from the title, so do that.
						var titleSplit = to.TrimStart(TextArrays.Colon).Split(TextArrays.Colon, 2);
						if (titleSplit.Length == 2 && interwikiPrefixes.Contains(titleSplit[0]))
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
					redirects[item.MustHaveString(fromName)] = toPage;
				}
			}

			return redirects;
		}

		public static RevisionItem GetRevision(this JToken token)
		{
			var content = token.ToNullableBCString("content");
			var revId = (long?)token["revid"] ?? 0;
			var sha1 = (string?)token["sha1"];
			if (sha1 != null)
			{
				if (sha1.TrimStart('0').Length == 0)
				{
					// If it's all zeroes, switch it to null and ignore it. This is caused by SHA-1 values beginning with either 0x or 0b, as documented here: https://bugs.php.net/bug.php?id=50175 and https://bugs.php.net/bug.php?id=55398.
					sha1 = null;
				}
				else if (content != null && content.GetHash(HashType.Sha1) != sha1)
				{
					// CONSIDER: This was changed from a warning to an exception. Should it be handled in Eve or allowed to fall through to the caller?
					throw new ChecksumException(CurrentCulture(EveMessages.RevisionSha1Failed, revId));
				}
			}

			return new RevisionItem(
				comment: (string?)token["comment"],
				content: content,
				contentFormat: (string?)token["contentformat"],
				contentModel: (string?)token["contentmodel"],
				flags: token.GetFlags(
					("anon", RevisionFlags.Anonymous),
					("commenthidden", RevisionFlags.CommentHidden),
					("minor", RevisionFlags.Minor),
					("sha1hidden", RevisionFlags.Sha1Hidden),
					("suppressed", RevisionFlags.Suppressed),
					("texthidden", RevisionFlags.TextHidden),
					("userhidden", RevisionFlags.UserHidden)),
				parentId: (long?)token["parentid"] ?? 0,
				parsedComment: (string?)token["parsedcomment"],
				parseTree: (string?)token["parsetree"],
				revisionId: revId,
				rollbackToken: (string?)token["rollbacktoken"],
				sha1: sha1,
				size: (long?)token["size"] ?? 0,
				tags: token["tags"].ToReadOnlyList<string>(),
				timestamp: (DateTime?)token["timestamp"],
				user: (string?)token["user"],
				userId: (long?)token["userid"] ?? -1);
		}

		public static List<RevisionItem> GetRevisions(this JToken token)
		{
			var revisions = new List<RevisionItem>();
			if (token?["revisions"] is JToken resultNode)
			{
				foreach (var revisionNode in resultNode)
				{
					revisions.Add(revisionNode.GetRevision());
				}
			}

			return revisions;
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

		public static WikiTitleItem GetWikiTitle(this JToken? result) => result == null ? throw new InvalidOperationException() : new WikiTitleItem((int)result.MustHave("ns"), result.MustHaveString("title"), (long?)result["pageid"] ?? 0);

		public static JToken MustHave(this JToken token, string name, [CallerMemberName] string caller = Unknown) => token[name] ?? throw MalformedException(name, token, caller);

		public static string MustHaveBCString(this JToken token, string name, [CallerMemberName]string caller = Unknown)
		{
			ThrowNull(token, nameof(token));
			var node = token[name] ?? token["*"] ?? throw MalformedException(name, token, caller);
			return (string?)node ?? throw MalformedException(name, token, caller);
		}

		public static DateTime MustHaveDate(this JToken token, string name, [CallerMemberName] string caller = Unknown)
		{
			ThrowNull(token, nameof(token));
			var node = token[name] ?? throw MalformedException(name, token, caller);
			return ToDate(node, caller);
		}

		public static List<T> MustHaveList<T>(this JToken token, string name, [CallerMemberName] string caller = Unknown) => token.MustHave(name, caller).ToList<T>();

		// "Text" instead of "String" so as not to conflict with any variant of ToString() that might be added to JToken later on.
		public static string MustHaveString(this JToken token, string name, [CallerMemberName] string caller = Unknown)
		{
			ThrowNull(token, nameof(token));
			var node = token[name] ?? throw MalformedException(name, token, caller);
			return (string?)node ?? throw MalformedException(name, token, caller);
		}

		public static bool ToBCBool(this JToken? token) =>
			token == null ? false :
			token.Type == JTokenType.Boolean ? (bool)token :
			token.Type == JTokenType.String ? true :
			throw MalformedException((token as JProperty)?.Name ?? Unknown, token);

		public static IReadOnlyDictionary<string, string?> ToBCDictionary(this JToken? token)
		{
			if (token == null)
			{
				return ImmutableDictionary<string, string?>.Empty;
			}

			if (token.Type == JTokenType.Array)
			{
				var dict = new Dictionary<string, string?>();
				foreach (var entry in token)
				{
					var key = (string)entry["name"]!;
					var value = (string?)entry["*"];

					dict.Add(key, value);
				}

				return dict.AsReadOnly();
			}

			return token.ToStringDictionary<string?>();
		}

		public static IEnumerable<(string key, JToken value)> ToBCIndexedList(this JToken? token, int formatVersion)
		{
			if (token == null)
			{
				yield break;
			}

			if (formatVersion == 2)
			{
				foreach (var node in token)
				{
					var useNode = (JProperty)node;
					if (useNode != null)
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

		public static List<T> ToList<T>(this JToken? token) => new List<T>(token?.Values<T>() ?? Array.Empty<T>());

		public static string? ToNullableBCString(this JToken token, string name)
		{
			ThrowNull(token, nameof(token));
			return (string?)(token[name] ?? token["*"]);
		}

		public static IReadOnlyList<T> ToReadOnlyList<T>(this JToken? token) => ToList<T>(token).AsReadOnly();

		public static Dictionary<string, TValue> ToStringDictionary<TValue>(this JToken? token)
		{
			var dict = new Dictionary<string, TValue>();
			if (token != null)
			{
				foreach (var item in token.Children<JProperty>())
				{
					dict.Add(item.Name, item.Value.ToObject<TValue>());
				}
			}

			return dict;
		}
		#endregion

		#region String Methods

		public static DateTime ToDate(this JToken? token, [CallerMemberName] string caller = Unknown)
		{
			var date = (string?)token;
			switch (date)
			{
				case null:
				case "":
					throw ArgumentNull(nameof(token));
				case "indefinite":
				case "infinite":
				case "infinity":
				case "never":
					return DateTime.MaxValue;
				default:
					return (
						DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var retval) ||
						DateTime.TryParseExact(date, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out retval))
							? retval
							: throw MalformedTypeException(nameof(DateTime), token, caller);
			}
		}

		/// <summary>Figures out what kind of date we're dealing with (ISO 8601, all digits, or infinite and its variants) and then returns the appropriate value.</summary>
		/// <param name="token">A string with the date.</param>
		/// <returns>A DateTime value or null. If the date evaluates to infinity, DateTime.MaxDate.</returns>
		public static DateTime? ToNullableDate(this JToken? token) => ((string?)token).ToNullableDate();

		/// <summary>Figures out what kind of date we're dealing with (ISO 8601, all digits, or infinite and its variants) and then returns the appropriate value.</summary>
		/// <param name="date">A string with the date.</param>
		/// <returns>A DateTime value or null. If the date evaluates to infinity, DateTime.MaxDate.</returns>
		public static DateTime? ToNullableDate(this string? date)
		{
			switch (date)
			{
				case null:
				case "":
					return null;
				case "indefinite":
				case "infinite":
				case "infinity":
				case "never":
					return DateTime.MaxValue;
				default:
					return (
						DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var retval) ||
						DateTime.TryParseExact(date, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out retval))
							? retval
							: null as DateTime?;
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

		#region Other Methods

		public static T GetUserData<T>(this T user, JToken result)
			where T : InternalUserItem
		{
			ThrowNull(user, nameof(user));
			if (result != null)
			{
				// ListAllUsers returns an empty string, ListUsers returns null, so check for both.
				var regDate = (string?)result["registration"];
				var rights = result["rights"];
				if (rights != null)
				{
					if (rights.Type == JTokenType.Array)
					{
						user.Rights = rights.ToReadOnlyList<string>();
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
				user.BlockExpiry = result["blockexpiry"].ToNullableDate();
				user.BlockHidden = result["hidden"].ToBCBool();
				user.EditCount = (long?)result["editcount"] ?? 0;
				user.Groups = result["groups"].ToReadOnlyList<string>();
				user.ImplicitGroups = result["implicitgroups"].ToReadOnlyList<string>();
				user.Registration = string.IsNullOrEmpty(regDate) ? null : (DateTime?)result["registration"];
			}

			return user;
		}
		#endregion
	}
}