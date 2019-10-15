#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	internal static class JTokenLogEvent
	{
		#region Fields
		private static readonly Regex ProtectionFinder = new Regex(@"\[(?<action>[^=]*?)=(?<restrictions>[^\]]*?)\] \((?<indef>indefinite|infinit[ey]|never)?(expires (?<expiry>.*?) \(UTC\))?\)", RegexOptions.Compiled);
		#endregion

		#region Internal Extension Methods
		internal static void ParseLogEvent(this JToken result, ILogEvents le, string logType, string logAction, ICollection<string> knownProps, bool hasUserIdFlag)
		{
			// hasUserIdFlag only necessary for list=logevents bug fix, can presumably be removed when we get to Json2 format.
			le.ExtraData = GetDictionary(result, le, logType, logAction, knownProps, hasUserIdFlag);
			le.LogType = logType;
			le.LogAction = logAction;
			le.LogEventFlags =
				result.GetFlag("actionhidden", LogEventFlags.ActionHidden) |
				result.GetFlag("commenthidden", LogEventFlags.CommentHidden) |
				result.GetFlag("suppressed", LogEventFlags.Suppressed) |
				result.GetFlag("anon", LogEventFlags.UserAnonymous) |
				result.GetFlag("userhidden", LogEventFlags.UserHidden);
			le.UserId = (long?)result["userid"] ?? -1;
			le.User = (string?)result["user"];
			le.Timestamp = (DateTime?)result["timestamp"];
			le.Comment = (string?)result["comment"];
			le.ParsedComment = (string?)result["parsedcomment"];
			le.LogId = (long?)result["logid"] ?? 0;
		}

		private static Dictionary<string, object?> GetDictionary(JToken result, ILogEvents le, string logType, string logAction, ICollection<string> knownProps, bool hasUserIdFlag)
		{
			// TODO: Look at this code again later and rewrite, especially if updated for a later version of MW. Bug fix code is for MW 1.24 and before. I think all the ExtraData stuff is too, or much of it, anyway. For 1.25+, everything goes to result["params"]. Just convert that to a dictionary and parse it.
			var parms = result["params"];
			var doBugFix = parms == null;
			var baseNode = parms ?? (logType == null ? result : result[logType]);
			var values = baseNode?.ToObject<Dictionary<string, object?>>() ?? new Dictionary<string, object?>();
			if (doBugFix && logType != null && baseNode != null)
			{
				foreach (var prop in result.Children<JProperty>())
				{
					if (!knownProps.Contains(prop.Name))
					{
						values.Add(prop.Name, prop.Value);
					}
				}
			}

			var dict = logType switch
			{
				"block" => ExtraDataBlock(values, logAction),
				"delete" => ExtraDataDelete(values, logAction),
				"suppress" => ExtraDataDelete(values, logAction),
				"merge" => ExtraDataMerge(values),
				"move" => ExtraDataMove(values),
				"newusers" => ExtraDataNewUsers(values),
				"pagelang" => ExtraDataPageLanguage(values),
				"patrol" => ExtraDataPatrol(values),
				"protect" => ExtraDataProtect(values, logAction),
				"rights" => ExtraDataRights(values),
				"upload" => ExtraDataUpload(values),
				_ => values,
			};

			if (doBugFix && le is LogEventsItem logEventsOutput && logEventsOutput.LogType == "newusers")
			{
				// Per https://phabricator.wikimedia.org/T73020
				switch (logEventsOutput.LogAction)
				{
					case "create":
						dict["createuserid"] = logEventsOutput.UserId;
						break;
					case "create2":
						if (hasUserIdFlag)
						{
							dict.Remove("createuserid");
						}
						else
						{
							dict["createuserid"] = logEventsOutput.UserId;
							logEventsOutput.UserId = -1;
						}

						break;
					default:
						break;
				}
			}

			return dict;
		}
		#endregion

		#region Private Methods
		private static Dictionary<string, object?> ExtraDataBlock(Dictionary<string, object?> values, string action) => action != "unblock" && values["duration"] != null
			? new Dictionary<string, object?>
			{
				{ "duration", (string?)values["duration"] },
				{ "flags", (string?)values["flags"] },
				{ "expiry", (DateTime?)values["expiry"] }
			}
			: new Dictionary<string, object?>();

		private static Dictionary<string, object?> ExtraDataDelete(Dictionary<string, object?> values, string action)
		{
			// TODO: This code *only* seems to support 1.24 and below. Is that right? That doesn't seem like what I would've wanted, but maybe I just forgot to come back to this.
			var dict = new Dictionary<string, object?>();
			var valOffset = '0';
			if (action == "event" || action == "revision")
			{
				var revisionType = (string?)values[valOffset.ToString()];
				switch (revisionType)
				{
					case "archive":
					case "filearchive":
					case "oldimage":
					case "revision":
						dict.Add("revisiontype", revisionType);
						valOffset++;
						break;
				}

				var value = values[valOffset.ToString()];
				if (value != null)
				{
					var ids = (string)value;
					var logIds = new List<long>();
					foreach (var commaSplit in ids.Split(TextArrays.Comma))
					{
						logIds.Add(long.Parse(commaSplit, CultureInfo.InvariantCulture));
					}

					dict.Add("logids", logIds);
				}

				value = values[valOffset++.ToString()];
				if (value != null)
				{
					dict.Add("old", LogEventGetRDType((string)value));
				}

				value = values[valOffset++.ToString()];
				if (value != null)
				{
					dict.Add("new", LogEventGetRDType((string)value));
				}
			}

			return dict;
		}

		private static Dictionary<string, object?> ExtraDataMerge(Dictionary<string, object?> values) => new Dictionary<string, object?>
		{
			{ "mergetitle", (string?)values["0"] },
			{ "mergetimestamp", ((string?)values["1"]).AsDate() }
		};

		private static Dictionary<string, object?> ExtraDataMove(Dictionary<string, object?> values) => new Dictionary<string, object?>
		{
			{ "suppressredirect", values["suppressredirect"] != null },
			{ "ns", (int?)values["new_ns"] },
			{ "title", (string?)values["new_title"] }
		};

		private static Dictionary<string, object?> ExtraDataNewUsers(Dictionary<string, object?> values)
		{
			var dict = new Dictionary<string, object?>();
			var value = values["userid"];
			if (value != null)
			{
				dict.Add("createuserid", (int)value);
			}

			return dict;
		}

		private static Dictionary<string, object?> ExtraDataPageLanguage(Dictionary<string, object?> values) => new Dictionary<string, object?>
		{
			{ "newlanguage", (string?)values["newlanguage"] },
			{ "oldlanguage", (string?)values["oldlanguage"] }
		};

		private static Dictionary<string, object?> ExtraDataPatrol(Dictionary<string, object?> values) => new Dictionary<string, object?>
		{
			{ "currentid", (long?)values["curid"] ?? 0 },
			{ "previousid", (long?)values["previd"] ?? 0 },
			{ "autopatrolled", (string?)values["auto"] == "1" }
		};

		private static Dictionary<string, object?> ExtraDataProtect(Dictionary<string, object?> values, string action)
		{
			var dict = new Dictionary<string, object?>();
			switch (action)
			{
				case "move_prot":
					dict.Add("movedpage", (string?)values["0"]);
					return dict;
				case "unprotect":
					return dict;
				default:
					var protections = new List<ProtectionsItem>();
					var matches = ProtectionFinder.Matches((string?)values["0"]);
					foreach (Match match in matches)
					{
						var groups = match.Groups;
						var protData = new ProtectionsItem()
						{
							Type = groups["action"].Value,
							Level = groups["restrictions"].Value,
						};
						protData.Expiry = groups["indef"].Success
							? null
							: (DateTime?)DateTime.Parse(groups["expiry"].Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

						protData.Cascading = !string.IsNullOrEmpty((string?)values["1"]);
						protections.Add(protData);
					}

					dict.Add("protections", protections);
					return dict;
			}
		}

		private static Dictionary<string, object?> ExtraDataRights(Dictionary<string, object?> values) => new Dictionary<string, object?>
		{
			{ "new", ParseRights((string?)values["new"]) },
			{ "old", ParseRights((string?)values["old"]) }
		};

		private static Dictionary<string, object?> ExtraDataUpload(Dictionary<string, object?> values) => new Dictionary<string, object?>
		{
			{ "sha1", (string?)values["img_sha1"] },
			{ "uploadtimestamp", (DateTime?)values["img_timestamp"] }
		};

		private static RevisionDeleteTypes LogEventGetRDType(string param)
		{
			var info = param.Split(TextArrays.EqualsSign);
			var type = info[info.Length - 1];
			return (RevisionDeleteTypes)int.Parse(type, CultureInfo.InvariantCulture);
		}

		private static IReadOnlyList<string> ParseRights(string? value) => value?.Split(TextArrays.CommaSpace, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
		#endregion
	}
}