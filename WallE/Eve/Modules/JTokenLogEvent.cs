#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Text.RegularExpressions;
	using Base;
	using Newtonsoft.Json.Linq;

	internal static class JTokenLogEvent
	{
		#region Fields
		private static Regex protectionFinder = new Regex(@"\[(?<action>.*?)=(?<restrictions>.*?)] \((?<indef>indefinite|infinity|never)?(expires (?<expiry>.*?) \(UTC\))?\)", RegexOptions.Compiled);
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
#pragma warning disable IDE0007 // Use implicit type
					foreach (JProperty prop in result)
#pragma warning restore IDE0007 // Use implicit type
					{
						if (!knownProps.Contains(prop.Name))
						{
							values.Add(prop.Name, prop.Value);
						}
					}
				}
			}

			var dict = GetExtraData(logType, logAction, values);
			var logEventsOutput = le as LogEventsItem;
			if (doBugFix && logEventsOutput != null && logEventsOutput.LogType == "newusers")
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
		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Code contains large switch. A routine for each switch might make it cleaner and less cyclomatically complex, but would also increase complexity in terms of re-passing a lot of info unnecessarily.")]
		private static Dictionary<string, object> GetExtraData(string type, string action, Dictionary<string, object> values)
		{
			var dict = new Dictionary<string, object>();
			switch (type)
			{
				case "block":
					if (action != "unblock" && values["duration"] != null)
					{
						dict.Add("duration", (string)values["duration"]);
						dict.Add("flags", (string)values["flags"]);
						dict.Add("expiry", (DateTime?)values["expiry"]);
					}

					break;
				case "delete":
				case "suppress":
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
						foreach (var value in ids.Split(','))
						{
							logIds.Add(long.Parse(value, CultureInfo.InvariantCulture));
						}

						dict.Add("logids", logIds);

						valOffset++;
						dict.Add("old", LogEventGetRDType((string)values[valOffset.ToStringInvariant()]));
						valOffset++;
						dict.Add("new", LogEventGetRDType((string)values[valOffset.ToStringInvariant()]));
					}

					break;
				case "merge":
					dict.Add("mergetitle", (string)values["0"]);
					dict.Add("mergetimestamp", ((string)values["1"]).AsDate());

					break;
				case "move":
					dict.Add("suppressredirect", values["suppressredirect"] != null);
					dict.Add("Newtonsoft", (int)values["new_ns"]);
					dict.Add("title", (string)values["new_title"]);

					break;
				case "newusers":
					if (values["userid"] != null)
					{
						dict.Add("createuserid", (int)values["userid"]);
					}

					break;
				case "pagelang":
					dict.Add("newlanguage", (string)values["newlanguage"]);
					dict.Add("oldlanguage", (string)values["oldlanguage"]);

					break;
				case "patrol":
					dict.Add("currentid", (long?)values["curid"] ?? 0);
					dict.Add("previousid", (long?)values["previd"] ?? 0);
					dict.Add("autopatrolled", (string)values["auto"] == "1");

					break;
				case "protect":
					switch (action)
					{
						case "move_prot":
							dict.Add("movedpage", (string)values["0"]);
							break;
						case "unprotect":
							break;
						default:
							var protections = new List<ProtectionsItem>();
#pragma warning disable IDE0007 // Use implicit type
							foreach (Match match in protectionFinder.Matches((string)values["0"]))
#pragma warning restore IDE0007 // Use implicit type
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

					break;
				case "rights":
					dict.Add("new", ParseRights((string)values["new"]));
					dict.Add("old", ParseRights((string)values["old"]));

					break;
				case "upload":
					dict.Add("sha1", (string)values["img_sha1"]);
					dict.Add("uploadtimestamp", (DateTime?)values["img_timestamp"]);

					break;
				default:
					dict = values;
					break;
			}

			return dict;
		}

		private static RevisionDeleteTypes LogEventGetRDType(string param)
		{
			var info = param.Split('=');
			var type = info[info.Length - 1];
			return (RevisionDeleteTypes)int.Parse(type, CultureInfo.InvariantCulture);
		}

		private static IReadOnlyList<string> ParseRights(string value) => value.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
		#endregion
	}
}