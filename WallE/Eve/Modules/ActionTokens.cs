#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionTokens : ActionModule<TokensInput, IReadOnlyDictionary<string, string>>
	{
		#region Constructors
		public ActionTokens(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 120;

		public override string Name { get; } = "tokens";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Get;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TokensInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("type", input.Types);
		}

		protected override IReadOnlyDictionary<string, string> DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new Dictionary<string, string>();
			result.AddPropertiesToDictionary(output);

			return output;
		}
		#endregion
	}
}