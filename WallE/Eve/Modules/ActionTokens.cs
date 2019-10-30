#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WikiCommon.RequestBuilder;
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
		public override int MinimumVersion => 120;

		public override string Name => "tokens";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Get;
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
			return result.ToStringDictionary<string>();
		}
		#endregion
	}
}