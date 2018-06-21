#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using Base;
	using Design;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using WikiCommon;
	using static Properties.EveMessages;
	using static WikiCommon.Globals;

	/// <summary>Any other API-related items that didn't warrant creation of their own static class.</summary>
	internal static class ParsingExtensions
	{
		#region Fields
		internal static Regex TooManyFinder { get; } = new Regex(@"Too many values .*?'(?<parameter>.*?)'.*?limit is (?<sizelimit>[0-9]+)", RegexOptions.Compiled);
		#endregion

		#region JToken Methods
		public static void AddToDictionary<TKey, TValue>(this JToken token, IDictionary<TKey, TValue> dict, string keyName, string valueName)
		{
			if (token != null)
			{
				foreach (var item in token)
				{
					if (item[keyName] != null)
					{
						var key = item[keyName].Value<TKey>();
						var value = item[valueName].Value<TValue>();
						dict.Add(key, value);
					}
				}
			}
		}

		// This routine makes the assumption that the value will always be one of { null, "", boolean }, since these are the only values that MediaWiki should ever emit, therefore no error checking is done.
		public static bool AsBCBool(this JToken value) =>
			value == null ? false :
			value.Type == JTokenType.String ? true :
			(bool)value;

		public static JToken AsBCContent(this JToken token, string index) => token[index] ?? token["*"];

		public static ReadOnlyDictionary<string, string> AsBCDictionary(this JToken token)
		{
			var dict = new Dictionary<string, string>();
			if (token != null)
			{
				if (token.Type == JTokenType.Array)
				{
					foreach (var entry in token)
					{
						var key = (string)entry["name"];
						var value = (string)entry["*"];

						dict.Add(key, value);
					}
				}
				else
				{
#pragma warning disable IDE0007 // Use implicit type
					foreach (JProperty entry in token)
#pragma warning restore IDE0007 // Use implicit type
					{
						dict.Add(entry.Name, (string)entry.Value);
					}
				}
			}

			return dict.AsReadOnly();
		}

		public static JToken AsBCSubContent(this JToken token) => token?.Type == JTokenType.Object ? token?["*"] : token;

		public static DateTime? AsDate(this JToken date) => ((string)date).AsDate();

		public static IReadOnlyList<T> AsReadOnlyList<T>(this JToken token) => token?.Values<T>().AsReadOnlyList() ?? new List<T>();

		public static IReadOnlyList<T> AsReadOnlyList<T>(this JToken token, string key)
		{
			if (token == null)
			{
				return new T[0];
			}

			var node = token[key];
			if (node == null)
			{
				return new T[0];
			}

			return node.Values<T>().AsReadOnlyList();
		}

		public static IReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(this JToken token)
		{
			if (token == null)
			{
				return EmptyReadOnlyDictionary<TKey, TValue>();
			}

			return token.ToObject<Dictionary<TKey, TValue>>().AsReadOnly();
		}

		public static IReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(this JToken token, string key)
		{
			if (token == null)
			{
				return EmptyReadOnlyDictionary<TKey, TValue>();
			}

			var node = token[key];
			if (node == null)
			{
				return EmptyReadOnlyDictionary<TKey, TValue>();
			}

			return node.ToObject<Dictionary<TKey, TValue>>().AsReadOnly();
		}

		public static ErrorItem GetError(this JToken result) => GetError(result, "code", "info");

		public static ErrorItem GetError(this JToken result, string codeName, string infoName)
		{
			if (result != null)
			{
				var code = (string)result[codeName];
				var info = (string)result[infoName];
				return (code ?? info) == null ? null : new ErrorItem(code, info);
			}

			return null;
		}

		public static T GetFlag<T>(this JToken result, string nodeName, T value) => result[nodeName].AsBCBool() ? value : default(T);

		public static List<InterwikiTitleItem> GetInterwikiLinks(this JToken result)
		{
			var output = new List<InterwikiTitleItem>();
			if (result != null)
			{
				foreach (var item in result)
				{
					var title = new InterwikiTitleItem()
					{
						InterwikiPrefix = (string)item["iw"],
						Title = (string)item["title"],
						Url = (Uri)item["url"],
					};
					output.Add(title);
				}
			}

			return output;
		}

		public static LanguageLinksItem GetLanguageLink(this JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var output = new LanguageLinksItem()
			{
				Language = (string)result["lang"],
				Url = (Uri)result["url"],
				Name = (string)result["langname"],
				Autonym = (string)result["autonym"],
				Title = (string)result["title"],
			};
			return output;
		}

		public static void GetRedirects(this JToken result, Dictionary<string, PageSetRedirectItem> redirects, WikiAbstractionLayer wal)
		{
			if (result != null)
			{
				foreach (var item in result)
				{
					var toPage = new PageSetRedirectItem();
					var from = (string)item["from"];
					toPage.Title = (string)item["to"];
					toPage.Fragment = (string)item["tofragment"];
					toPage.Interwiki = (string)item["tointerwiki"];
					if (toPage.Interwiki == null && wal.SiteVersion < 125)
					{
						var titleSplit = toPage.Title.TrimStart(':').Split(new[] { ':' }, 2);
						if (titleSplit.Length == 2 && wal.InterwikiPrefixes.Contains(titleSplit[0]))
						{
							toPage.Interwiki = titleSplit[0];
							toPage.Title = titleSplit[1];
						}
					}

					var gi = item.ToObject<Dictionary<string, object>>();
					gi.Remove("from");
					gi.Remove("to");
					gi.Remove("tofragment");
					gi.Remove("tointerwiki");

					toPage.GeneratorInfo = gi;

					redirects[from] = toPage;
				}
			}
		}

		public static RevisionsItem GetRevision(this JToken result, string pageTitle)
		{
			var revision = new RevisionsItem()
			{
				Comment = (string)result["comment"],
				Content = (string)result.AsBCContent("content"),
				ContentFormat = (string)result["contentformat"],
				ContentModel = (string)result["contentmodel"],
				Flags =
					GetFlag(result, "anon", RevisionFlags.Anonymous) |
					GetFlag(result, "commenthidden", RevisionFlags.CommentHidden) |
					GetFlag(result, "minor", RevisionFlags.Minor) |
					GetFlag(result, "sha1hidden", RevisionFlags.Sha1Hidden) |
					GetFlag(result, "suppressed", RevisionFlags.Suppressed) |
					GetFlag(result, "texthidden", RevisionFlags.TextHidden) |
					GetFlag(result, "userhidden", RevisionFlags.UserHidden),
				ParentId = (long?)result["parentid"] ?? 0,
				ParsedComment = (string)result["parsedcomment"],
				ParseTree = (string)result["parsetree"],
				RevisionId = (long?)result["revid"] ?? 0,
				RollbackToken = (string)result["rollbacktoken"],
				Sha1 = (string)result["sha1"],
				Size = (long?)result["size"] ?? 0,
				Tags = result.AsReadOnlyList<string>("tags"),
				Timestamp = (DateTime?)result["timestamp"],
				User = (string)result["user"],
				UserId = (long?)result["userid"] ?? -1,
			};

			if (revision.Sha1 != null && revision.Content != null && revision.Content.GetHash(HashType.Sha1) != revision.Sha1)
			{
				// TODO: This was changed from a warning to an exception. Consider whether to handle the exception in Eve or hand it off to the caller.
				throw new ChecksumException(CurrentCulture(RevisionSha1Failed, revision.RevisionId, pageTitle));
			}

			return revision;
		}

		public static void GetUser(this JToken result, IUser user)
		{
			user.UserId = (long?)result["userid"] ?? 0;
			user.Name = (string)result["name"];
			user.EditCount = (long?)result["editcount"] ?? 0;
			user.BlockId = (long?)result["blockid"] ?? 0;
			user.BlockedBy = (string)result["blockedby"];
			user.BlockedById = (long?)result["blockedbyid"] ?? 0;
			user.BlockTimestamp = (DateTime?)result["blockedtimestamp"];
			user.BlockReason = (string)result["blockreason"];
			user.BlockExpiry = result["blockexpiry"].AsDate();
			user.BlockHidden = result["hidden"].AsBCBool();

			// ListAllUsers returns an empty string, ListUsers returns null, so check for both.
			var regDate = (string)result["registration"];
			user.Registration = string.IsNullOrEmpty(regDate) ? null : (DateTime?)result["registration"];
			user.Groups = result.AsReadOnlyList<string>("groups");
			user.ImplicitGroups = result.AsReadOnlyList<string>("implicitgroups");
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
					var rightsList = rights.ToObject<Dictionary<int, string>>().Values;
					user.Rights = rightsList.AsReadOnlyList();
				}
			}
		}

		public static IReadOnlyList<WarningsItem> GetWarnings(this JToken result)
		{
			var list = new List<WarningsItem>();
			if (result != null)
			{
				foreach (var item in result)
				{
					var warning = new WarningsItem()
					{
						Type = (string)item["type"],
						Message = (string)item["message"],
						Parameters = item["params"].ToObject<IEnumerable<object>>().AsReadOnlyList(),
					};
					list.Add(warning);
				}
			}

			return list.AsReadOnly();
		}

		public static WikiTitleItem GetWikiTitle(this JToken result)
		{
			var title = new WikiTitleItem();
			title.GetWikiTitle(result);
			return title;
		}
		#endregion

		#region ITitle Methods
		public static void GetWikiTitle(this ITitle title, JToken result)
		{
			title.Namespace = (int?)result["ns"];
			title.PageId = (long?)result["pageid"] ?? 0;
			title.Title = (string)result["title"];
		}
		#endregion

		#region ITitleOnly Methods
		public static void GetWikiTitle(this ITitleOnly title, JToken result)
		{
			title.Namespace = (int?)result["ns"];
			title.Title = (string)result["title"];
		}
		#endregion

		#region String Methods

		/// <summary>Figures out what kind of date we're dealing with (ISO 8601, all digits, or infinite and its variants) and then return the appropriate value.</summary>
		/// <param name="date">A string with the date.</param>
		/// <returns>A DateTime value or null. If the date evaluates to infinity, DateTime.MaxDate.</returns>
		public static DateTime? AsDate(this string date)
		{
			var dateText = date;
			switch (dateText)
			{
				case null:
				case "":
				case "infinite":
				case "infinity":
				case "never":
				case "indefinite":
					return null;
				default:
					if (dateText.EndsWith("Z", StringComparison.Ordinal))
					{
						return DateTime.Parse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
					}

					return DateTime.ParseExact(dateText, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
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
	}
}