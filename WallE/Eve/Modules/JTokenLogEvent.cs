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
		internal static void ParseLogEvent(this JToken result, ILogEvents le, string logType, string logAction, HashSet<string> knownProps, bool hasUserIdFlag)
		{
			// hasUserIdFlag only necessary for list=logevents bug fix, can presumably be removed when we get to Json2 format.
			Dictionary<string, object> values;
			var doBugFix = true;
			if (result["params"] != null)
			{
				doBugFix = false;
				values = result["params"].ToObject<Dictionary<string, object>>();
			}
			else if (logType == null)
			{
				values = result.ToObject<Dictionary<string, object>>();
			}
			else
			{
				values = result[logType].ToObject<Dictionary<string, object>>();
				if (values == null)
				{
					values = new Dictionary<string, object>();
					foreach (var prop in result.Children<JProperty>())
					{
						if (!knownProps.Contains(prop.Name))
						{
							values.Add(prop.Name, prop.Value);
						}
					}
				}
			}

			var dict = GetExtraData(logType, logAction, values);
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

			le.GetWikiTitle(result);
			le.ExtraData = dict;
			le.LogType = logType;
			le.LogAction = logAction;
			le.LogEventFlags =
				result.GetFlag("actionhidden", LogEventFlags.ActionHidden) |
				result.GetFlag("commenthidden", LogEventFlags.CommentHidden) |
				result.GetFlag("suppressed", LogEventFlags.Suppressed) |
				result.GetFlag("anon", LogEventFlags.UserAnonymous) |
				result.GetFlag("userhidden", LogEventFlags.UserHidden);
			le.UserId = (long?)result["userid"] ?? -1;
			le.User = (string)result["user"];
			le.Timestamp = (DateTime?)result["timestamp"];
			le.Comment = (string)result["comment"];
			le.ParsedComment = (string)result["parsedcomment"];
			le.LogId = (long?)result["logid"] ?? 0;
		}
		#endregion

		#region Private Methods
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Complexity warning is a lie. To be fixed in an upcoming Roslyn update.")]
		private static Dictionary<string, object> GetExtraData(string type, string action, Dictionary<string, object> values)
		{
			// Used to be a switch, but converted to if/else if block because that's what happens internally anyway, and CA1502 was falsely triggering with bizarrely high complexities on the string switch.
			var dict = new Dictionary<string, object>();
			switch (type)
			{
				case "block":
					ExtraDataBlock(dict, values, action);
					break;
				case "delete":
				case "suppress":
					ExtraDataDelete(dict, values, action);
					break;
				case "merge":
					ExtraDataMerge(dict, values);
					break;
				case "move":
					ExtraDataMove(dict, values);
					break;
				case "newusers":
					ExtraDataNewUsers(dict, values);
					break;
				case "pagelang":
					ExtraDataPageLanguage(dict, values);
					break;
				case "patrol":
					ExtraDataPatrol(dict, values);
					break;
				case "protect":
					ExtraDataProtect(dict, values, action);
					break;
				case "rights":
					ExtraDataRights(dict, values);
					break;
				case "upload":
					ExtraDataUpload(dict, values);
					break;
				default:
					dict = values;
					break;
			}

			return dict;
		}

		private static void ExtraDataBlock(Dictionary<string, object> dict, Dictionary<string, object> values, string action)
		{
			if (action != "unblock" && values["duration"] != null)
			{
				dict.Add("duration", (string)values["duration"]);
				dict.Add("flags", (string)values["flags"]);
				dict.Add("expiry", (DateTime?)values["expiry"]);
			}
		}

		private static void ExtraDataDelete(Dictionary<string, object> dict, Dictionary<string, object> values, string action)
		{
			var valOffset = 0;
			if (action == "event" || action == "revision")
			{
				var revisionType = (string)values[valOffset.ToStringInvariant()];
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

				var ids = (string)values[valOffset.ToStringInvariant()];
				var logIds = new List<long>();
				foreach (var value in ids.Split(TextArrays.Comma))
				{
					logIds.Add(long.Parse(value, CultureInfo.InvariantCulture));
				}

				dict.Add("logids", logIds);

				valOffset++;
				dict.Add("old", LogEventGetRDType((string)values[valOffset.ToStringInvariant()]));
				valOffset++;
				dict.Add("new", LogEventGetRDType((string)values[valOffset.ToStringInvariant()]));
			}
		}

		private static void ExtraDataMerge(Dictionary<string, object> dict, Dictionary<string, object> values)
		{
			dict.Add("mergetitle", (string)values["0"]);
			dict.Add("mergetimestamp", ((string)values["1"]).AsDate());
		}

		private static void ExtraDataMove(Dictionary<string, object> dict, Dictionary<string, object> values)
		{
			dict.Add("suppressredirect", values["suppressredirect"] != null);
			dict.Add("Newtonsoft", (int)values["new_ns"]);
			dict.Add("title", (string)values["new_title"]);
		}

		private static void ExtraDataNewUsers(Dictionary<string, object> dict, Dictionary<string, object> values)
		{
			if (values["userid"] != null)
			{
				dict.Add("createuserid", (int)values["userid"]);
			}
		}

		private static void ExtraDataPageLanguage(Dictionary<string, object> dict, Dictionary<string, object> values)
		{
			dict.Add("newlanguage", (string)values["newlanguage"]);
			dict.Add("oldlanguage", (string)values["oldlanguage"]);
		}

		private static void ExtraDataPatrol(Dictionary<string, object> dict, Dictionary<string, object> values)
		{
			dict.Add("currentid", (long?)values["curid"] ?? 0);
			dict.Add("previousid", (long?)values["previd"] ?? 0);
			dict.Add("autopatrolled", (string)values["auto"] == "1");
		}

		private static void ExtraDataProtect(Dictionary<string, object> dict, Dictionary<string, object> values, string action)
		{
			switch (action)
			{
				case "move_prot":
					dict.Add("movedpage", (string)values["0"]);
					break;
				case "unprotect":
					break;
				default:
					var protections = new List<ProtectionsItem>();
					foreach (var match in ProtectionFinder.Matches((string)values["0"]) as IReadOnlyList<Match>)
					{
						var groups = match.Groups;
						var protData = new ProtectionsItem()
						{
							Type = groups["action"].Value,
							Level = groups["restrictions"].Value,
						};
						if (groups["indef"].Success)
						{
							protData.Expiry = null;
						}
						else
						{
							protData.Expiry = DateTime.Parse(groups["expiry"].Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
						}

						protData.Cascading = !string.IsNullOrEmpty((string)values["1"]);
						protections.Add(protData);
					}

					dict.Add("protections", protections);

					break;
			}
		}

		private static void ExtraDataRights(Dictionary<string, object> dict, Dictionary<string, object> values)
		{
			dict.Add("new", ParseRights((string)values["new"]));
			dict.Add("old", ParseRights((string)values["old"]));
		}

		private static void ExtraDataUpload(Dictionary<string, object> dict, Dictionary<string, object> values)
		{
			dict.Add("sha1", (string)values["img_sha1"]);
			dict.Add("uploadtimestamp", (DateTime?)values["img_timestamp"]);
		}

		private static RevisionDeleteTypes LogEventGetRDType(string param)
		{
			var info = param.Split(TextArrays.EqualsSign);
			var type = info[info.Length - 1];
			return (RevisionDeleteTypes)int.Parse(type, CultureInfo.InvariantCulture);
		}

		private static IReadOnlyList<string> ParseRights(string value) => value.Split(TextArrays.CommaSpace, StringSplitOptions.RemoveEmptyEntries);
		#endregion
	}
}