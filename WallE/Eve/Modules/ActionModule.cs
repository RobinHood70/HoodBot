#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Eve;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	public abstract class ActionModule : IModule
	{
		#region Constructors
		protected ActionModule(WikiAbstractionLayer wal) => this.Wal = wal.NotNull(nameof(wal));
		#endregion

		#region Public Abstract Properties
		public abstract int MinimumVersion { get; }

		public abstract string Name { get; }
		#endregion

		#region Public Virtual Properties
		public virtual string FullPrefix => this.Prefix;

		public virtual string Prefix => string.Empty;
		#endregion

		#region Protected Properties
		protected int SiteVersion => this.Wal.SiteVersion;

		protected WikiAbstractionLayer Wal { get; }
		#endregion

		#region Protected Abstract Properties
		protected abstract RequestType RequestType { get; }
		#endregion

		#region Protected Virtual Properties
		protected virtual bool GetTimeStamp => true;
		#endregion

		#region Protected Static Methods
		protected static JToken ToJson(string response)
		{
			using var responseReader = new StringReader(response.NotNull(nameof(response)));
			using var reader = new JsonTextReader(responseReader) { DateParseHandling = DateParseHandling.None };
			return JToken.Load(reader); // using JToken.Load instead of .Parse so we can ignore date parsing.
		}
		#endregion

		#region Protected Methods
		protected Request CreateBaseRequest()
		{
			var wal = this.Wal; // Just to make code more readable.
			return new Request(this.Wal.EntryPoint, this.RequestType, this.SiteVersion >= 128)
				.AddIfNotNull("action", this.Name)
				.AddIfNotNull("uselang", wal.UseLanguage)
				.AddFormat("json")
				.AddIf("formatversion", wal.DetectedFormatVersion, wal.DetectedFormatVersion > 1)
				.Add("utf8", wal.Utf8 && wal.DetectedFormatVersion != 2)
				.AddIf("assert", wal.Assert!, (wal.ValidStopCheckMethods & StopCheckMethods.Assert) != 0 && !string.IsNullOrEmpty(wal.Assert))
				.AddIfNotNullIf("assertuser", wal.CurrentUserInfo?.Name, (wal.ValidStopCheckMethods & StopCheckMethods.UserNameCheck) != 0 && wal.SiteVersion >= 128)
				.AddIf("maxlag", wal.MaxLag, wal.SupportsMaxLag && wal.MaxLag != 0) // Can be -1 for testing, so check != 0 rather than > 0
				.Add("curtimestamp", this.GetTimeStamp && wal.SiteVersion >= 124);
		}

		protected void DeserializeAction(JToken result)
		{
			result.ThrowNull(nameof(result));

			// TODO: Add multiple-error support here (errorformat=raw) using new GetErrors() function.
			if (result["error"].GetError() is ErrorItem error)
			{
#pragma warning disable SA1513 // Closing brace should be followed by blank line // False hit after closing brace
				throw error.Code switch
				{
					"assertbotfailed" or "assertuserfailed" or "assertnameduserfailed" => new StopException(error.Info),
					"editconflict" => new EditConflictException(),
					_ => WikiException.General(error.Code, error.Info),
				};
#pragma warning restore SA1513 // Closing brace should be followed by blank line
			}

			if (result["warnings"] is JToken warnings)
			{
				foreach (var warning in warnings.Children<JProperty>())
				{
					if (warning.First is JToken descNode)
					{
						var description = descNode.MustHaveBCString("warnings");
						foreach (var line in description.Split(TextArrays.LineFeed))
						{
							this.AddWarning(warning.Name, line);
						}
					}
				}
			}

			if (result["debuginfo"] is JToken debugInfo)
			{
				var includes = new List<DebugInfoInclude>();
				foreach (var include in debugInfo.MustHave("includes"))
				{
					includes.Add(new DebugInfoInclude(
						name: include.MustHaveString("name"),
						size: include.MustHaveString("size")));
				}

				var logs = new List<DebugInfoLog>();
				foreach (var log in debugInfo.MustHave("log"))
				{
					logs.Add(new DebugInfoLog(
						caller: log.MustHaveString("caller"),
						logType: log.MustHaveString("type"),
						message: log.MustHaveString("msg")));
				}

				var queries = new List<DebugInfoQuery>();
				foreach (var query in debugInfo.MustHave("queries"))
				{
					queries.Add(new DebugInfoQuery(
						function: query.MustHaveString("function"),
						isMaster: query["master"].GetBCBool(),
						runTime: (double?)query["time"] ?? 0,
						sql: query.MustHaveString("sql")));
				}

				var requestNode = debugInfo.MustHave("request");
				var fullHost = this.Wal.EntryPoint.AbsoluteUri.Replace(this.Wal.EntryPoint.AbsolutePath, string.Empty, StringComparison.Ordinal);
				var request = new DebugInfoRequest(
						headers: requestNode.MustHave("headers").GetStringDictionary<string>(),
						method: requestNode.MustHaveString("method"),
						parameters: requestNode.MustHave("params").GetStringDictionary<string>(),
						url: new Uri(fullHost + requestNode.MustHaveString("url")));

				this.Wal.DebugInfo = new DebugInfoResult(
					debugLog: debugInfo.MustHaveList<string>("debugLog"),
					elapsedTime: (double?)debugInfo["time"] ?? 0,
					gitBranch: (string?)debugInfo["gitBranch"].IgnoreFalse(),
					gitRevision: (string?)debugInfo["gitRevision"].IgnoreFalse(),
					gitViewUrl: (Uri?)debugInfo["gitViewUrl"].IgnoreFalse(),
					includes: includes,
					log: logs,
					mwVersion: debugInfo.MustHaveString("mwVersion"),
					phpEngine: debugInfo.MustHaveString("phpEngine"),
					phpVersion: debugInfo.MustHaveString("phpVersion"),
					queries: queries,
					request: request);
			}

			if (result["curtimestamp"] is JToken timestamp)
			{
				this.Wal.CurrentTimestamp = timestamp.GetNullableDate();
			}

			this.DeserializeActionExtra(result);
		}

		protected int FindRequiredNamespace(string title)
		{
			var nsSplit = title.NotNull(nameof(title)).Split(TextArrays.Colon, 2);
			if (nsSplit.Length == 2)
			{
				var nsText = nsSplit[0];
				foreach (var ns in this.Wal.Namespaces)
				{
					if (string.Equals(nsText, ns.Value.Name, StringComparison.Ordinal))
					{
						return ns.Key;
					}
				}
			}

			// No namespaces matched, or title didn't have a colon, so it must be in Main space.
			return 0;
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual void AddWarning(string from, string text)
		{
			if (!string.IsNullOrEmpty(text) && !this.HandleWarning(from, text))
			{
				this.Wal.AddWarning(from, text);
			}
		}

		protected virtual void BeforeSubmit()
		{
			if (this.SiteVersion != 0 && this.MinimumVersion > this.SiteVersion)
			{
				throw new InvalidOperationException(Globals.CurrentCulture(EveMessages.ActionNotSupported, this.GetType().Name));
			}
		}

		protected virtual void DeserializeActionExtra(JToken result)
		{
		}

		// Swallow all token warnings. Currently emitted primarily by queries, but also by ApiTokens.
		protected virtual bool HandleWarning(string from, string text) =>
			text
				.NotNull(nameof(text))
				.StartsWith("Action '", StringComparison.Ordinal) &&
			text.EndsWith("' is not allowed for the current user", StringComparison.Ordinal);
		#endregion
	}
}
