﻿namespace RobinHood70.WallE.Eve;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Design;
using RobinHood70.WikiCommon.RequestBuilder;
using static RobinHood70.WallE.Eve.Exceptions;

/// <summary>Extensions that help with parsing JSON data returned by WallE.Eve.</summary>
/// <remarks>These extensions are made public to allow external classes to use the same methods as are used internally. Note that some of these methods are highly specific, but are nevertheless available here for the sake of simplicity, since multiple modules use them. Most methods will gracefully handle <see langword="null"/> tokens, typically by returning a null result. The parameter nullability indicates which is expected; those methods which expect a non-null token will also note that in the remarks.</remarks>
public static class ParsingExtensions
{
	#region Enum Methods

	/// <summary>Gets a series of flag nodes, regardless of format version.</summary>
	/// <typeparam name="T">The enumeration type.</typeparam>
	/// <param name="value">The flags to modify.</param>
	/// <param name="token">The token to examine. (Will be searched for the "case" value.</param>
	/// <param name="flag">The flag to be added if the "case" value evaluates to "case-sensitive".</param>
	/// <returns>The modified enumeration value.</returns>
	public static T AddCaseFlag<T>(this T value, JToken token, T flag)
		where T : struct, Enum
	{
		if (token == null)
		{
			return value;
		}

		if (((string?)token["case"]).OrdinalEquals("case-sensitive"))
		{
			var longVal = Convert.ToUInt64(value, CultureInfo.InvariantCulture);
			var longFlag = Convert.ToUInt64(flag, CultureInfo.InvariantCulture);
			return (T)Enum.ToObject(typeof(T), longVal | longFlag);
		}

		return value;
	}
	#endregion

	#region JToken Methods

	/// <summary>Gets a boolean from the current token, regardless of format version.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns><see langword="true"/> if the token provided is <see langword="true"/> or an empty string; <see langword="false"/> if the token is <see langword="false"/> or null.</returns>
	/// <exception cref="WikiException">The node data was not convertible to a boolean value.</exception>
	[SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Default case is intended to cover all remaining possibilities.")]
	public static bool GetBCBool(this JToken? token) => token?.Type switch
	{
		JTokenType.Boolean => (bool)token,
		JTokenType.String => true,
		null => false,
		_ => throw MalformedException(token is JProperty jProperty ? jProperty.Name : Globals.Unknown, token)
	};

	/// <summary>Gets a dictionary of string keys and values from the current token, regardless of format version.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>A read-only dictionary of the keys and values in the node.</returns>
	public static IReadOnlyDictionary<string, string?> GetBCDictionary(this JToken? token)
	{
		if (token == null)
		{
			return ImmutableDictionary<string, string?>.Empty;
		}

		if (token.Type == JTokenType.Array)
		{
			Dictionary<string, string?> dict = new(StringComparer.Ordinal);
			foreach (var entry in token)
			{
				var key = (string?)entry["name"] ?? string.Empty;
				var value = (string?)entry["*"];

				dict.Add(key, value);
			}

			return dict.AsReadOnly();
		}

		return token.GetStringDictionary<string?>();
	}

	/// <summary>Gets the correct data node, regardless of format version.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>The current JToken node or the "*" subnode of it.</returns>
	/// <remarks>The naming is taken from MediaWiki's ApiResult::META_BC_SUBELEMENTS.</remarks>
	public static JToken? GetBCSubElements(this JToken? token) => token?.Type == JTokenType.Object ? token["*"] : token;

	/// <summary>Gets a date from the current token.</summary>
	/// <param name="token">The token to examine.</param>
	/// <param name="caller">The caller name (automatically populated).</param>
	/// <returns>System.DateTime.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the token has an empty date value.</exception>
	/// <exception cref="WikiException">Thrown when the value of the token is not a recognized date format.</exception>
	public static DateTime GetDate(this JToken? token, [CallerMemberName] string caller = Globals.Unknown)
	{
		static DateTime? ParseWikiDate(string date) => (
			DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var retval) ||
			DateTime.TryParseExact(date, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out retval))
				? retval
				: null;

		var date = (string?)token;
		var dateValue = date switch
		{
			null or "" => throw MalformedTypeException(nameof(DateTime), token, caller),
			"indefinite" or "infinite" or "infinity" or "never" => DateTime.MaxValue,
			_ => ParseWikiDate(date) ?? throw MalformedTypeException(nameof(DateTime), token, caller),
		};
		return dateValue;
	}

	/// <summary>Gets a standard code/info-formatted error.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>An <see cref="ErrorItem"/> with the error details. Can be null only if the error node parameter was null.</returns>
	[return: NotNullIfNotNull(nameof(token))]
	public static ErrorItem? GetError(this JToken? token) => GetError(token, "code", "info");

	/// <summary>Gets a standard code/info-formatted error with custom names.</summary>
	/// <param name="token">The token to examine.</param>
	/// <param name="codeName">Name of the code node.</param>
	/// <param name="infoName">Name of the information node.</param>
	/// <returns>An <see cref="ErrorItem"/> with the error details. Can be null only if the error node parameter was null.</returns>
	/// <remarks>This is used for sub-structures, such as move results, where the code/info nodes have prefixes.</remarks>
	[return: NotNullIfNotNull(nameof(token))]
	public static ErrorItem? GetError(this JToken? token, string codeName, string infoName) =>
		token == null || token[codeName] == null
			? null
			: new ErrorItem(token.MustHaveString(codeName), token.MustHaveString(infoName));

	/// <summary>Gets multiple error item nodes.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>A list of <see cref="ErrorItem"/>s.</returns>
	// Currently unused - intended for later development because newer versions of the API can return multiple errors in a single response.
	public static IReadOnlyList<ErrorItem> GetErrors(this JToken? token)
	{
		List<ErrorItem> list = [];
		if (token != null)
		{
			foreach (var node in token)
			{
				if (node.GetError() is ErrorItem error)
				{
					list.Add(error);
				}
			}
		}

		return list;
	}

	/// <summary>Gets a series of flag nodes, regardless of format version.</summary>
	/// <typeparam name="T">The enumeration type. Should normally be inferred.</typeparam>
	/// <param name="token">The token to examine.</param>
	/// <param name="nodeNames">The node names and the value to assign if the node evaluates to <see langword="true"/>.</param>
	/// <example><c>result.GetFlags(("new", EditFlags.New), ("nochange", EditFlags.NoChange))</c>.</example>
	/// <returns>The combined enumeration values.</returns>
	public static T GetFlags<T>(this JToken? token, params (string Key, T Value)[] nodeNames)
			where T : struct, Enum
	{
		ulong retval = 0;
		if (token != null && nodeNames != null)
		{
			foreach (var (key, value) in nodeNames)
			{
				retval |= token[key].GetBCBool() ? Convert.ToUInt64(value, CultureInfo.InvariantCulture) : 0;
			}
		}

		return (T)Enum.ToObject(typeof(T), retval);
	}

	/// <summary>Gets a list of interwiki links.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>A list containing the interwiki links.</returns>
	public static IReadOnlyList<InterwikiTitleItem> GetInterwikiLinks(this JToken? token)
	{
		List<InterwikiTitleItem> output = [];
		if (token != null)
		{
			foreach (var item in token)
			{
				output.Add(new InterwikiTitleItem(item.MustHaveString("iw"), item.MustHaveString("title"), (Uri?)item["url"]));
			}
		}

		return output;
	}

	/// <summary>Gets a language link.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>A <see cref="LanguageLinksItem"/>.</returns>
	[return: NotNullIfNotNull(nameof(token))]
	public static LanguageLinksItem? GetLanguageLink(this JToken? token) => token == null
		? null
		: new LanguageLinksItem(
			language: token.MustHaveString("lang"),
			title: token.MustHaveString("title"),
			autonym: (string?)token["autonym"],
			name: (string?)token["langname"],
			url: (Uri?)token["url"]);

	/// <summary>Gets a list of the specified type from an array node.</summary>
	/// <typeparam name="T">The type to convert the values to.</typeparam>
	/// <param name="token">The token to examine.</param>
	/// <returns>An <see cref="IReadOnlyList{T}"/> of the specified type.</returns>
	public static IReadOnlyList<T> GetList<T>(this JToken? token)
	{
		List<T> retval = [];
		if (token != null)
		{
			foreach (var value in token.Values<T>())
			{
				if (value is not null)
				{
					retval.Add(value);
				}
			}
		}

		return retval;
	}

	/// <summary>Gets a nullable string, regardless of format version.</summary>
	/// <param name="token">The token to examine.</param>
	/// <param name="name">The name.</param>
	/// <returns>A nullable string.</returns>
	/// <remarks>The token provided must not be <see langword="null"/>.</remarks>
	public static string? GetNullableBCString(this JToken token, string name)
	{
		ArgumentNullException.ThrowIfNull(token);
		var node = token[name] ?? token["*"];
		return (string?)node;
	}

	/// <summary>Figures out what kind of date we're dealing with (ISO 8601, all digits, or infinite and its variants) and then returns the appropriate value.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>A <see cref="DateTime"/> value or null. If the date evaluates to infinity, <see cref="DateTime.MaxValue"/>.</returns>
	public static DateTime? GetNullableDate(this JToken? token) => ((string?)token).GetNullableDate();

	/// <summary>Gets a collection of redirects.</summary>
	/// <param name="token">The token to examine.</param>
	/// <param name="redirects">The dictionary of redirects to populate.</param>
	/// <param name="interwikiPrefixes">The interwiki prefixes.</param>
	/// <param name="siteVersion">The site version.</param>
	public static void GetRedirects(this JToken? token, IDictionary<string, PageSetRedirectItem> redirects, IReadOnlySet<string> interwikiPrefixes, int siteVersion)
	{
		const string fromName = "from";
		const string toName = "to";
		const string toFragmentName = "tofragment";
		const string toInterwikiName = "tointerwiki";

		ArgumentNullException.ThrowIfNull(redirects);
		ArgumentNullException.ThrowIfNull(interwikiPrefixes);
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
					if (titleSplit.Length == 2 && interwikiPrefixes.Contains(titleSplit[0], StringComparer.Ordinal))
					{
						interwiki = titleSplit[0];
						to = titleSplit[1];
					}
				}

				var gi = item.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>(StringComparer.Ordinal);
				gi.Remove(fromName);
				gi.Remove(toName);
				gi.Remove(toFragmentName);
				gi.Remove(toInterwikiName);

				PageSetRedirectItem toPage = new(
					title: to,
					fragment: (string?)item[toFragmentName],
					interwiki: interwiki,
					generatorInfo: gi);
				redirects[item.MustHaveString(fromName)] = toPage;
			}
		}
	}

	/// <summary>Gets a revision.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>A <see cref="RevisionItem"/>.</returns>
	/// <remarks>The token provided must not be <see langword="null"/>.</remarks>
	public static RevisionItem GetRevision(this JToken token)
	{
		ArgumentNullException.ThrowIfNull(token);
		var flags = token.GetFlags(
			("anon", RevisionFlags.Anonymous),
			("commenthidden", RevisionFlags.CommentHidden),
			("minor", RevisionFlags.Minor),
			("sha1hidden", RevisionFlags.Sha1Hidden),
			("suppressed", RevisionFlags.Suppressed),
			("texthidden", RevisionFlags.TextHidden),
			("userhidden", RevisionFlags.UserHidden),
			("badcontentformat", RevisionFlags.BadContentFormat),
			("badcontentformatforparsetree", RevisionFlags.BadContentFormatForParseTree));
		var sha1 = (string?)token["sha1"];
		if (sha1 is not null && sha1.TrimStart('0').Length == 0)
		{
			// If it's all zeroes, switch it to null. This is caused by SHA-1 values beginning with either 0x or 0b, as documented here: https://bugs.php.net/bug.php?id=50175 and https://bugs.php.net/bug.php?id=55398.
			sha1 = null;
		}

		var size = (long?)token["size"] ?? 0;
		var slotsToken = token["slots"];
		var slots = new Dictionary<string, SlotItem>(StringComparer.Ordinal);
		if (slotsToken is null)
		{
			var mappedFlags =
				(flags.HasAnyFlag(RevisionFlags.BadContentFormat) ? SlotFlags.BadContentFormat : SlotFlags.None) |
				(flags.HasAnyFlag(RevisionFlags.Sha1Hidden) ? SlotFlags.Sha1Hidden : SlotFlags.None) |
				(flags.HasAnyFlag(RevisionFlags.TextHidden) ? SlotFlags.TextHidden : SlotFlags.None);
			var slot = new SlotItem(
				content: token.GetNullableBCString("content"),
				contentFormat: (string?)token["contentformat"],
				contentModel: (string?)token["contentmodel"],
				flags: mappedFlags,
				sha1: sha1,
				size: size);
			slots.Add("main", slot);
		}
		else
		{
			foreach (var slotProperty in slotsToken.Children<JProperty>())
			{
				var slotValue = slotProperty.Value;
				var slotFlags = slotValue.GetFlags(
					("badcontentformat", SlotFlags.BadContentFormat),
					("missing", SlotFlags.Missing),
					("nosuchsection", SlotFlags.NoSuchSection),
					("sha1hidden", SlotFlags.Sha1Hidden),
					("texthidden", SlotFlags.TextHidden),
					("textmissing", SlotFlags.TextMissing));
				var slot = new SlotItem(
					content: slotValue.GetNullableBCString("content"),
					contentFormat: (string?)slotValue["contentformat"],
					contentModel: (string?)slotValue["contentmodel"],
					flags: slotFlags,
					sha1: (string?)slotValue["sha1"],
					size: (long?)slotValue["size"] ?? 0);
				slots.Add(slotProperty.Name, slot);
			}
		}

		var revId = (long?)token["revid"] ?? 0;

		return new RevisionItem(
			comment: (string?)token["comment"],
			flags: flags,
			parentId: (long?)token["parentid"] ?? 0,
			parsedComment: (string?)token["parsedcomment"],
			parseTree: (string?)token["parsetree"],
			revisionId: revId,
			rollbackToken: (string?)token["rollbacktoken"],
			sha1: sha1,
			size: size,
			slots: slots.AsReadOnly(),
			tags: token["tags"].GetList<string>(),
			timestamp: (DateTime?)token["timestamp"],
			user: (string?)token["user"],
			userId: (long?)token["userid"] ?? -1);
	}

	/// <summary>Gets a list of revisions.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>A list of <see cref="RevisionItem"/>s.</returns>
	public static IReadOnlyList<RevisionItem> GetRevisions(this JToken? token)
	{
		List<RevisionItem> revisions = [];
		if (token?["revisions"] is JToken resultNode)
		{
			foreach (var revisionNode in resultNode)
			{
				revisions.Add(revisionNode.GetRevision());
			}
		}

		return revisions;
	}

	/// <summary>Gets a dictionary with string keys and the specified value type from the current token.</summary>
	/// <typeparam name="TValue">The type to convert values to.</typeparam>
	/// <param name="token">The token to examine.</param>
	/// <returns>A <see cref="Dictionary{TKey, TValue}"/> with a <see cref="string"/> key and values of the specified type.</returns>
	public static IReadOnlyDictionary<string, TValue> GetStringDictionary<TValue>(this JToken? token)
	{
		if (token == null)
		{
			return ImmutableDictionary<string, TValue>.Empty;
		}

		Dictionary<string, TValue> dict = new(StringComparer.Ordinal);
		foreach (var item in token.Children<JProperty>())
		{
			dict.Add(item.Name, item.Value.ToObject<TValue>()!);
		}

		return dict;
	}

	/// <summary>Gets common user data to be passed to other user-based objects.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>An <see cref="UserItem"/> to be passed to the derived user object.</returns>
	/// <remarks>The token provided must not be <see langword="null"/>.</remarks>
	/// <exception cref="WikiException">Thrown when the user rights token is not in either of the expected formats.</exception>
	public static UserItem GetUser(this JToken token)
	{
		ArgumentNullException.ThrowIfNull(token);

		// Somewhere prior to 1.22, rights lists could be returned as a numbered key-value pair instead of a straight-forward string array, so this handles that situation and converts it to the expected type.
		IReadOnlyList<string>? userRights = null;
		if (token["rights"] is JToken rights)
		{
			if (rights.Type == JTokenType.Array)
			{
				userRights = rights.GetList<string>();
			}
			else
			{
				var dict = rights.ToObject<Dictionary<int, string>>();
				userRights = dict != null
					? dict.Values.AsReadOnlyList()
					: throw MalformedTypeException("rights", token);
			}
		}

		return new UserItem(
			userId: (long?)(token["userid"] ?? token["id"]) ?? 0,
			name: token.MustHaveString("name"),
			blockedBy: (string?)token["blockedby"],
			blockedById: (long?)token["blockedbyid"] ?? 0,
			blockExpiry: token["blockexpiry"].GetNullableDate(),
			blockHidden: token["hidden"].GetBCBool(),
			blockId: (long?)token["blockid"] ?? 0,
			blockReason: (string?)token["blockreason"],
			blockTimestamp: (DateTime?)token["blockedtimestamp"],
			editCount: (long?)token["editcount"] ?? 0,
			groups: token["groups"].GetList<string>(),
			implicitGroups: token["implicitgroups"].GetList<string>(),
			registration: (token["registration"] ?? token["registrationdate"]).GetNullableDate(),
			rights: userRights);
	}

	/// <summary>Gets a list of warnings.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>System.Collections.Generic.IReadOnlyList&lt;RobinHood70.WallE.Base.WarningsItem&gt;.</returns>
	public static IReadOnlyList<WarningsItem> GetWarnings(this JToken? token)
	{
		List<WarningsItem> list = [];
		if (token != null)
		{
			foreach (var item in token)
			{
				WarningsItem warning = new((string?)item["type"] ?? string.Empty, (string?)item["message"], (item["params"]?.ToObject<IEnumerable<object>>()).AsReadOnlyList());
				list.Add(warning);
			}
		}

		return list.AsReadOnly();
	}

	/// <summary>Gets a wiki title.</summary>
	/// <param name="token">The token to examine.</param>
	/// <returns>A <see cref="WikiTitleItem"/>.</returns>
	/// <remarks>The token provided must not be <see langword="null"/>.</remarks>
	/// <exception cref="ArgumentNullException">Thrown when the token is null.</exception>
	public static WikiTitleItem GetWikiTitle(this JToken token)
	{
		ArgumentNullException.ThrowIfNull(token);
		return new(
			ns: (int)token.MustHave("ns"),
			title: token.MustHaveString("title"),
			pageId: (long?)token["pageid"] ?? 0);
	}

	/// <summary>Ignores a token if it is boolean and evaluates to <see langword="false"/>.</summary>
	/// <param name="token">The token.</param>
	/// <returns>The original token or null if the token was <see langword="false"/>.</returns>
	/// <remarks>This is used for rare cases where formatversion=2 returns false when a value should really be <see langword="null"/>.</remarks>
	public static JToken? IgnoreFalse(this JToken? token) => token == null || (token.Type == JTokenType.Boolean && !(bool)token)
		? null
		: token;

	/// <summary>Ensures that the token is non-null and contains a non-null string.</summary>
	/// <param name="token">The token to examine.</param>
	/// <param name="caller">The caller name (automatically populated).</param>
	/// <returns>A <see cref="string"/> representing the value in the node.</returns>
	/// <remarks>The token provided must not be <see langword="null"/> (either the token itself or the string value).</remarks>
	/// <exception cref="WikiException">Thrown when the token cannot be converted to a string.</exception>
	public static string MustBeString(this JToken token, [CallerMemberName] string caller = Globals.Unknown) => (string?)token ?? throw MalformedTypeException(nameof(String), token, caller);

	/// <summary>Ensures that the token has a subnode with the given name and returns it.</summary>
	/// <param name="token">The token to examine.</param>
	/// <param name="name">The name to check.</param>
	/// <param name="caller">The caller name (automatically populated).</param>
	/// <returns>The named subnode.</returns>
	/// <exception cref="WikiException">Thrown when the token does not have a field with the specified name.</exception>
	public static JToken MustHave(this JToken token, string name, [CallerMemberName] string caller = Globals.Unknown) => token?[name] ?? throw MalformedException(name, token, caller);

	/// <summary>Ensures that the token has a non-null string node with either the given name or "*", and returns the value of that node.</summary>
	/// <param name="token">The token to examine.</param>
	/// <param name="name">The name.</param>
	/// <param name="caller">The caller name (automatically populated).</param>
	/// <returns>The value <see cref="string"/>.</returns>
	/// <exception cref="WikiException">Thrown when the token does not have a string value at either the named location or in the "*" entry.</exception>
	public static string MustHaveBCString(this JToken token, string name, [CallerMemberName] string caller = Globals.Unknown)
	{
		ArgumentNullException.ThrowIfNull(token);
		var node = token[name] ?? token["*"] ?? throw MalformedException(name, token, caller);
		return (string?)node ?? throw MalformedException(name, token, caller);
	}

	/// <summary>Ensures that the token has a date node with the given name and returns the value of the node.</summary>
	/// <param name="token">The token to examine.</param>
	/// <param name="name">The name.</param>
	/// <param name="caller">The caller name (automatically populated).</param>
	/// <returns>System.DateTime.</returns>
	/// <exception cref="WikiException">Thrown when the token does not have a date value at the named location.</exception>
	public static DateTime MustHaveDate(this JToken token, string name, [CallerMemberName] string caller = Globals.Unknown)
	{
		ArgumentNullException.ThrowIfNull(token);
		var node = token[name] ?? throw MalformedException(name, token, caller);
		return GetDate(node, caller);
	}

	/// <summary>Ensures that the token has a list node with elements of the given type and returns a list with the value of the elements.</summary>
	/// <typeparam name="T">The element type.</typeparam>
	/// <param name="token">The token to examine.</param>
	/// <param name="name">The name.</param>
	/// <param name="caller">The caller name (automatically populated).</param>
	/// <returns>A list of the specified type.</returns>
	public static IReadOnlyList<T> MustHaveList<T>(this JToken token, string name, [CallerMemberName] string caller = Globals.Unknown) => token.MustHave(name, caller).GetList<T>();

	/// <summary>Ensures that the token has a non-null string node with the given name and returns the value of that node.</summary>
	/// <param name="token">The token to examine.</param>
	/// <param name="name">The name.</param>
	/// <param name="caller">The caller name (automatically populated).</param>
	/// <returns>A <see cref="string"/> with the value of the node.</returns>
	/// <exception cref="WikiException">Thrown when the token does not have a string value at the named location.</exception>
	public static string MustHaveString(this JToken token, string name, [CallerMemberName] string caller = Globals.Unknown)
	{
		ArgumentNullException.ThrowIfNull(token);
		var node = token[name] ?? throw MalformedException(name, token, caller);
		return (string?)node ?? throw MalformedException(name, token, caller);
	}
	#endregion

	#region String Methods

	/// <summary>Figures out what kind of date we're dealing with (ISO 8601, all digits, or infinite and its variants) and then returns the appropriate value.</summary>
	/// <param name="date">A string with the date.</param>
	/// <returns>A DateTime value or null. If the date evaluates to infinity, DateTime.MaxDate.</returns>
	public static DateTime? GetNullableDate(this string? date) => date switch
	{
		null or "" => null,
		"indefinite" or "infinite" or "infinity" or "never" => DateTime.MaxValue,
		_ => (
			DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var retval) ||
			DateTime.TryParseExact(date, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out retval))
				? retval
				: null,
	};
	#endregion

	#region Request Methods

	/// <summary>Builds the revision portion of a request.</summary>
	/// <param name="request">The request.</param>
	/// <param name="input">The input.</param>
	/// <param name="siteVersion">The site version.</param>
	/// <returns>The original <see cref="Request"/>.</returns>
	public static Request BuildRevisions(this Request request, IRevisionsInput input, int siteVersion)
	{
		ArgumentNullException.ThrowIfNull(input);
		ArgumentNullException.ThrowIfNull(request);
		var props = input.Properties;
		if (siteVersion >= 132)
		{
			request.AddTemplatedKeys("slots", input.Slots, s => s.Name);
			request.AddTemplatedValues("contentformat-$1", input.Slots, s => s.Name, s => s.ContentFormat);
		}
		else
		{
			if (props.HasAnyFlag(RevisionsProperties.SlotSha1))
			{
				props |= RevisionsProperties.Sha1;
				props &= ~RevisionsProperties.SlotSha1;
			}

			if (props.HasAnyFlag(RevisionsProperties.SlotSize))
			{
				props |= RevisionsProperties.Size;
				props &= ~RevisionsProperties.SlotSize;
			}

			if (input.Slots is IEnumerable<SlotInput> slots)
			{
				foreach (var slot in slots)
				{
					request.AddIfNotNull("contentformat", slot.ContentFormat);
					break; // Only add first slot
				}
			}
		}

		request
			.AddFlags("prop", props)
			.Add("expandtemplates", input.ExpandTemplates)
			.Add("generatexml", input.GenerateXml)
			.Add("parse", input.Parse)
			.AddIf("section", input.Section, input.Section >= 0)
			.AddIfNotNull("diffto", MediaWikiGlobal.GetDiffToValue(input.DiffTo))
			.AddIfNotNull("difftotext", input.DiffToText)
			.AddIf("difftotextpst", input.DiffToTextPreSaveTransform, siteVersion >= 127)
			.Add("start", input.Start)
			.Add("end", input.End)
			.AddIf("dir", "newer", input.SortAscending)
			.AddIfNotNull(input.ExcludeUser ? "excludeuser" : "user", input.User);

		return request;
	}
	#endregion
}