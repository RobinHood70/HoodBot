#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.IO;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WallE.Properties.EveMessages;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class ActionModule<TInput, TOutput> : IActionModule<TInput, TOutput>
		where TInput : class
		where TOutput : class
	{
		#region Constructors
		protected ActionModule(WikiAbstractionLayer wal)
		{
			ThrowNull(wal, nameof(wal));
			this.Wal = wal;
			this.SiteVersion = wal.SiteVersion;
		}
		#endregion

		#region Public Abstract Properties
		public abstract int MinimumVersion { get; }

		public abstract string Name { get; }
		#endregion

		#region Public Virtual Properties
		public virtual string FullPrefix { get; } = string.Empty;
		#endregion

		#region Protected Properties
#if DEBUG
		protected Request Request { get; private set; }

		protected JToken Response { get; private set; }

#endif
		protected int SiteVersion { get; }

		protected WikiAbstractionLayer Wal { get; }
		#endregion

		#region Protected Abstract Properties
		protected abstract RequestType RequestType { get; }
		#endregion

		#region Protected Virtual Properties
		protected virtual bool Continues { get; } = true;

		protected virtual bool ForceCustomDeserialization { get; } = false;

		protected virtual string ResultName => this.Name;

		protected virtual StopCheckMethods StopMethods => this.Wal.StopCheckMethods;
		#endregion

		#region Public Methods
		public Request CreateRequest(TInput input)
		{
			ThrowNull(input, nameof(input));
			var wal = this.Wal;
			var request = new Request(this.Wal.Uri, this.RequestType, this.SiteVersion >= 128);
			request
				.AddIfNotNull("action", this.Name)
				.AddIf("uselang", wal.UseLanguage, wal.UseLanguage != null)
				.AddFormat("json")
				.AddIf("formatversion", wal.DetectedFormatVersion, wal.DetectedFormatVersion > 1)
				.Add("utf8", wal.Utf8 && wal.DetectedFormatVersion != 2)
				.AddIf("assert", wal.Assert, !string.IsNullOrEmpty(wal.Assert) && this.StopMethods.HasFlag(StopCheckMethods.Assert))
				.AddIf("assertuser", wal.UserName, this.StopMethods.HasFlag(StopCheckMethods.UserNameCheck) && this.SiteVersion >= 128)
				.AddIf("maxlag", wal.MaxLag, wal.SupportsMaxLag && wal.MaxLag != 0) // Can be -1 for testing, so check != 0 rather than > 0
				.Add("curtimestamp", this.SiteVersion >= 124)
				.Prefix = this.FullPrefix;
			this.BuildRequestLocal(request, input);
			request.Prefix = string.Empty;

			return request;
		}

		public TOutput Deserialize(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			this.DeserializeParent(parent);
			var result = parent[this.ResultName];
			if (result != null && result.Type != JTokenType.Null)
			{
				return this.DeserializeResult(result);
			}

			return null;
		}

		public TOutput Submit(TInput input)
		{
			this.Wal.ClearWarnings();
			this.BeforeSubmit(input);
			var output = this.SubmitInternal(input);
			this.AfterSubmit();
			return output;
		}
		#endregion

		#region Protected Methods
		protected TOutput SubmitInternal(TInput input)
		{
			var request = this.CreateRequest(input);
#if DEBUG
			this.Request = request;
#endif

			var response = this.Wal.SendRequest(request);
			if (response == null)
			{
				return null;
			}

			if (this.ForceCustomDeserialization)
			{
				return this.DeserializeCustom(response);
			}

			JToken jsonResponse;
			using (var responseReader = new StringReader(response))
			using (var reader = new JsonTextReader(responseReader))
			{
				try
				{
					// using JToken.Load instead of .Parse so we can ignore date parsing.
					reader.DateParseHandling = DateParseHandling.None;
					jsonResponse = JToken.Load(reader);
				}
				catch (JsonReaderException)
				{
					// Invalid JSON, try DeserializeCustom in case response could be either/or, or programmer forgot to mark it as forced custom deserialization.
					return this.DeserializeCustom(response);
				}
			}

#if DEBUG
			this.Response = jsonResponse;
#endif

			// This allows modules like OpenSearch to work correctly. If it returns an object with error info, the standard deserialization routine kicks in, while the custom one will kick in if it gets an array.
			if (jsonResponse.Type == JTokenType.Object)
			{
				return this.Deserialize(jsonResponse);
			}
			else if (jsonResponse.Type != JTokenType.Array || (jsonResponse as JArray).Count > 0)
			{
				// Deserialize unless it was just an empty array.
				return this.DeserializeCustom(jsonResponse);
			}

			return null;
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void BuildRequestLocal(Request request, TInput input);

		protected abstract TOutput DeserializeResult(JToken result);
		#endregion

		#region Protected Virtual Methods
		protected virtual void AddWarning(string from, string text) => this.Wal.AddWarning(from, text);

		protected virtual void AfterSubmit()
		{
			// Changes here may also need to be reflected in ActionQuery.AfterSubmit(), which cannot call this without triggering a second talk check.
			if (this.Wal.BreakRecursionAfterSubmit)
			{
				// Necessary because the custom stop check would become recursive if it called on any other modules.
				return;
			}

			this.Wal.BreakRecursionAfterSubmit = true;
			if (this.StopMethods.HasFlag(StopCheckMethods.Custom) && (this.Wal.CustomStopCheck?.Invoke() == true))
			{
				this.Wal.BreakRecursionAfterSubmit = false;
				throw new StopException(CustomStopCheckFailed);
			}

			if (this.StopMethods.HasFlag(StopCheckMethods.TalkCheckNonQuery))
			{
				var input = new UserInfoInput() { Properties = UserInfoProperties.HasMsg };
				this.Wal.UserInfo(input);
			}

			this.Wal.BreakRecursionAfterSubmit = false;
		}

		protected virtual void BeforeSubmit(TInput input)
		{
			if (this.SiteVersion != 0 && this.MinimumVersion > this.SiteVersion)
			{
				throw new InvalidOperationException(CurrentCulture(ActionNotSupported, this.GetType().Name));
			}
		}

		protected virtual TOutput DeserializeCustom(string result)
		{
			if (result != null && result.Contains("$wgEnableAPI"))
			{
				throw WikiException.General(WikiAbstractionLayer.ApiDisabledCode, CurrentCulture(ApiDisabled));
			}
			else
			{
				throw new WikiException(CurrentCulture(ResultInvalid));
			}
		}

		// This version is for responses like OpenSearch where the Json should be valid, but is an array rather than an object.
		protected virtual TOutput DeserializeCustom(JToken result) => throw new WikiException(CurrentCulture(ResultInvalid));

		protected virtual void DeserializeParent(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			var error = parent["error"];
			if (error != null)
			{
				var code = (string)error["code"];
				var info = (string)error["info"];
				switch (code)
				{
					case "assertbotfailed":
					case "assertuserfailed":
					case "assertnameduserfailed":
						throw new StopException(info);
					case "editconflict":
						throw new EditConflictException();
					default:
						throw WikiException.General(code, info);
				}
			}

			var warnings = parent["warnings"];
			if (warnings != null)
			{
				foreach (var warning in warnings.Children<JProperty>())
				{
					var description = (string)warning.First.AsBCContent("warnings");

					foreach (var line in description.Split(TextArrays.LineFeed))
					{
						this.AddWarning(warning.Name, line);
					}
				}
			}

			this.Wal.CurrentTimestamp = (DateTime?)parent["curtimestamp"];
		}
		#endregion
	}
}