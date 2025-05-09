﻿namespace RobinHood70.WallE.Eve.Modules;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.WallE.Base;
using static RobinHood70.WallE.Eve.ParsingExtensions;

internal static class JTokenLogEvent
{
	#region Fields
	private static readonly Regex ProtectionFinder = new(@"\[(?<action>[^=]*?)=(?<restrictions>[^\]]*?)\] \((?<indef>indefinite|infinit[ey]|never)?(expires (?<expiry>.*?) \(UTC\))?\)", RegexOptions.Compiled | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	#endregion

	#region Internal Extension Methods
	internal static void ParseLogEvent(this JToken result, LogEvent le, string prefix, ICollection<string> knownProps, bool hasUserIdFlag)
	{
		le.PageId = (long?)result["pageid"] ?? 0;
		le.LogType = (string?)result[prefix + "type"];
		le.LogAction = (string?)result[prefix + "action"];
		le.ExtraData = new ExtraDataParser(result, le, le.LogType, le.LogAction, knownProps, hasUserIdFlag).Result;
		le.LogEventFlags = result.GetFlags(
			("actionhidden", LogEventFlags.ActionHidden),
			("commenthidden", LogEventFlags.CommentHidden),
			("suppressed", LogEventFlags.Suppressed),
			("anon", LogEventFlags.UserAnonymous),
			("userhidden", LogEventFlags.UserHidden));
		le.UserId = (long?)result["userid"] ?? -1;
		le.User = (string?)result["user"];
		le.Timestamp = (DateTime?)result["timestamp"];
		le.Comment = (string?)result["comment"];
		le.ParsedComment = (string?)result["parsedcomment"];
		le.LogId = (long?)result["logid"] ?? 0;
	}
	#endregion

	#region Private Static Classes
	private static RevisionDeleteTypes LogEventGetRDType(string param)
	{
		ArgumentNullException.ThrowIfNull(param);
		var info = param.Split(TextArrays.EqualsSign);
		var type = info[^1];
		return (RevisionDeleteTypes)int.Parse(type, CultureInfo.InvariantCulture);
	}

	private static IReadOnlyList<string> ParseRights(string? value) => value is null
		? []
		: value.Split(TextArrays.CommaSpace, StringSplitOptions.RemoveEmptyEntries).AsReadOnlyList();
	#endregion

	#region Private Classes

	private sealed class ExtraDataParser
	{
		// TODO: Look at this code again later and rewrite, especially if updated for a later version of MW. Bug fix code is for MW 1.24 and before. I think all the ExtraData stuff is too, or much of it, anyway. For 1.25+, everything goes to result["params"]. Just convert that to a dictionary and parse it.
		#region Fields
		private readonly JToken parms;
		private readonly string? logAction;
		#endregion

		#region Constructors

		// hasUserIdFlag only necessary for list=logevents bug fix, can presumably be removed when we get to Json2 format.
		public ExtraDataParser(JToken result, LogEvent le, string? logType, string? logAction, ICollection<string> knownProps, bool hasUserIdFlag)
		{
			ArgumentNullException.ThrowIfNull(result);
			this.logAction = logAction;

			bool doBugFix;
			if (result["params"] is JToken parms)
			{
				this.parms = parms;
				doBugFix = true;
			}
			else
			{
				this.parms = result;
				doBugFix = false;
			}

			if (logType != null && result[logType] is JToken logParms)
			{
				this.parms = logParms;
			}

			switch (logType)
			{
				case "block":
					this.ExtraDataBlock();
					break;
				case "delete":
				case "suppress":
					this.ExtraDataDelete();
					break;
				case "merge":
					this.ExtraDataMerge();
					break;
				case "move":
					this.ExtraDataMove();
					break;
				case "newusers":
					this.ExtraDataNewUsers();
					break;
				case "pagelang":
					this.ExtraDataPageLanguage();
					break;
				case "patrol":
					this.ExtraDataPatrol();
					break;
				case "protect":
					this.ExtraDataProtect();
					break;
				case "rights":
					this.ExtraDataRights();
					break;
				case "upload":
					this.ExtraDataUpload();
					break;
				default:
					if (logType != null)
					{
						this.ExtraDataGeneric(knownProps);
					}

					break;
			}

			if (doBugFix && le is LogEventsItem logEventsOutput && logEventsOutput.LogType.OrdinalEquals("newusers"))
			{
				// Per https://phabricator.wikimedia.org/T73020
				switch (logEventsOutput.LogAction)
				{
					case "create":
						this.Result["createuserid"] = logEventsOutput.UserId;
						break;
					case "create2":
						if (hasUserIdFlag)
						{
							this.Result.Remove("createuserid");
						}
						else
						{
							this.Result["createuserid"] = logEventsOutput.UserId;
							logEventsOutput.UserId = -1;
						}

						break;
					default:
						break;
				}
			}
		}
		#endregion

		#region Public Properties
		public Dictionary<string, object?> Result { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);
		#endregion

		#region Private Methods
		private void ExtraDataGeneric(ICollection<string> knownProps)
		{
			foreach (var prop in this.parms.Children<JProperty>())
			{
				if (!knownProps.Contains(prop.Name, StringComparer.Ordinal))
				{
					this.Result.Add(prop.Name, prop.Value.ToObject<object>());
				}
			}
		}

		private void ExtraDataBlock()
		{
			if (!this.logAction.OrdinalEquals("unblock") && this.parms["duration"] != null)
			{
				this.Result.Add("duration", (string?)this.parms["duration"]);
				this.Result.Add("flags", (string?)this.parms["flags"]);
				this.Result.Add("expiry", (DateTime?)this.parms["expiry"]);
			}
		}

		private void ExtraDataDelete()
		{
			// TODO: This code *only* seems to support 1.24 and below. Is that right? That doesn't seem like what I would've wanted, but maybe I just forgot to come back to this.
			var valOffset = '0';
			if (this.logAction.OrdinalEquals("event") || this.logAction.OrdinalEquals("revision"))
			{
				var offsetText = new string(valOffset, 1);
				var revisionType = (string?)this.parms[offsetText];
				switch (revisionType)
				{
					case "archive":
					case "filearchive":
					case "oldimage":
					case "revision":
						this.Result.Add("revisiontype", revisionType);
						valOffset++;
						offsetText = new string(valOffset, 1);
						break;
					default:
						break;
				}

				if (this.parms[offsetText] is JToken logIdsNode && (string?)logIdsNode is string ids)
				{
					List<long> logIds = [];
					foreach (var commaSplit in ids.Split(TextArrays.Comma))
					{
						logIds.Add(long.Parse(commaSplit, CultureInfo.InvariantCulture));
					}

					this.Result.Add("logids", logIds);
				}

				offsetText = new string(++valOffset, 1);
				if (this.parms[offsetText] is JToken oldNode &&
					(string?)oldNode is string oldText)
				{
					this.Result.Add("old", LogEventGetRDType(oldText));
				}

				offsetText = new string(++valOffset, 1);
				if (this.parms[offsetText] is JToken newNode &&
					(string?)newNode is string newText)
				{
					this.Result.Add("new", LogEventGetRDType(newText));
				}
			}
		}

		private void ExtraDataMerge()
		{
			this.Result.Add("mergetitle", (string?)this.parms["0"]);
			this.Result.Add("mergetimestamp", this.parms["1"].GetNullableDate());
		}

		private void ExtraDataMove()
		{
			this.Result.Add("suppressredirect", this.parms["suppressredirect"] != null);
			this.Result.Add("ns", (int?)(this.parms["target_ns"] ?? this.parms["new_ns"])); // MoveLog ?? RC
			this.Result.Add("title", (string?)(this.parms["target_title"] ?? this.parms["new_title"]));
		}

		private void ExtraDataNewUsers()
		{
			var value = this.parms["userid"];
			if (value != null)
			{
				this.Result.Add("createuserid", (int)value);
			}
		}

		private void ExtraDataPageLanguage()
		{
			this.Result.Add("newlanguage", (string?)this.parms["newlanguage"]);
			this.Result.Add("oldlanguage", (string?)this.parms["oldlanguage"]);
		}

		private void ExtraDataPatrol()
		{
			this.Result.Add("currentid", (long?)this.parms["curid"] ?? 0);
			this.Result.Add("previousid", (long?)this.parms["previd"] ?? 0);
			this.Result.Add("autopatrolled", ((string?)this.parms["auto"]).OrdinalEquals("1"));
		}

		private void ExtraDataProtect()
		{
			if (this.logAction.OrdinalEquals("move_prot"))
			{
				this.Result.Add("movedpage", (string?)this.parms["0"]);
			}
			else if (!this.logAction.OrdinalEquals("unprotect"))
			{
				List<ProtectionsItem> protections = [];
				if ((string?)this.parms["0"] is string parm0)
				{
					IEnumerable<Match> matches = ProtectionFinder.Matches(parm0);
					foreach (var match in matches)
					{
						var groups = match.Groups;
						var expiry = groups["indef"].Success
							? null
							: groups["expiry"].Value.GetNullableDate();
						var cascading = !string.IsNullOrEmpty((string?)this.parms["1"]);

						protections.Add(new ProtectionsItem(
							type: groups["action"].Value,
							level: groups["restrictions"].Value,
							expiry: expiry,
							cascading: cascading,
							source: null));
					}
				}

				this.Result.Add("protections", protections);
			}
		}

		private void ExtraDataRights()
		{
			this.Result.Add("new", ParseRights((string?)this.parms["new"]));
			this.Result.Add("old", ParseRights((string?)this.parms["old"]));
		}

		private void ExtraDataUpload()
		{
			this.Result.Add("sha1", (string?)this.parms["img_sha1"]);
			this.Result.Add("uploadtimestamp", (DateTime?)this.parms["img_timestamp"]);
		}
		#endregion
	}
	#endregion
}