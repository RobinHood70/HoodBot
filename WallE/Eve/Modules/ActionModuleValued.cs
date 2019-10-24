#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Diagnostics.CodeAnalysis;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class ActionModuleValued<TInput, TOutput> : ActionModule
		where TInput : class
		where TOutput : class
	{
		#region Constructors
		protected ActionModuleValued(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Protected Virtual Properties
		protected virtual bool ForceCustomDeserialization => false;
		#endregion

		#region Public Methods
		public virtual TOutput Submit(TInput input)
		{
			this.Wal.ClearWarnings();
			this.BeforeSubmit();
			var request = this.CreateRequest(input);
			var response = this.Wal.SendRequest(request);
			return this.ParseResponse(response);
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void BuildRequestLocal(Request request, TInput input);

		protected abstract TOutput DeserializeResult(JToken result);
		#endregion

		#region Protected Virtual Methods
		[DoesNotReturn]
		protected virtual TOutput DeserializeCustom(string? result)
		{
			// Note that result will not yet have been checked for null in this version of deserialization.
			if (result != null && result.Contains("$wgEnableAPI"))
			{
				throw WikiException.General(WikiAbstractionLayer.ApiDisabledCode, CurrentCulture(EveMessages.ApiDisabled));
			}

			throw new WikiException(CurrentCulture(EveMessages.ResultInvalid));
		}

		// This version is for responses like OpenSearch where the Json should be valid, but is an array rather than an object.
		[DoesNotReturn]
		protected virtual TOutput DeserializeCustom(JToken result) => throw new WikiException(CurrentCulture(EveMessages.ResultInvalid));
		#endregion

		#region Private Methods
		private Request CreateRequest(TInput input)
		{
			ThrowNull(input, nameof(input));
			var request = this.CreateBaseRequest();
			request.Prefix = this.FullPrefix;
			this.BuildRequestLocal(request, input);
			request.Prefix = string.Empty;

			return request;
		}

		private TOutput Deserialize(JToken parent)
		{
			ThrowNull(parent, nameof(parent));
			this.DeserializeParent(parent);
			if (parent[this.Name] is JToken result && result.Type != JTokenType.Null)
			{
				return this.DeserializeResult(result) ?? throw WikiException.General("null-result", this.Name + " was found in the results, but the deserializer returned null.");
			}

			throw WikiException.General("no-result", "The expected result node, " + this.Name + ", was not found.");
		}

		private TOutput ParseResponse(string? response)
		{
			if (this.ForceCustomDeserialization)
			{
				return this.DeserializeCustom(response);
			}

			try
			{
				// DeserializeCustom allows modules like OpenSearch to work correctly. If it returns an object with error info, the standard deserialization routine kicks in, while the custom one will kick in if it gets an array.
				var jsonResponse = ToJson(response);
				return jsonResponse.Type == JTokenType.Object ? this.Deserialize(jsonResponse) : this.DeserializeCustom(jsonResponse);
			}
			catch (JsonReaderException)
			{
				// Invalid JSON, try DeserializeCustom in case response could be either/or, or programmer forgot to mark it as forced custom deserialization.
				return this.DeserializeCustom(response);
			}
		}
		#endregion
	}
}