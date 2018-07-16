#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class MetaTokens : QueryModule<TokensInput, IDictionary<string, string>>
	{
		#region Constructors
		public MetaTokens(WikiAbstractionLayer wal, TokensInput input)
			: base(wal, input, new Dictionary<string, string>())
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 120;

		public override string Name { get; } = "tokens";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType { get; } = "meta";

		protected override string Prefix { get; } = string.Empty;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TokensInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("type", input.Types);
		}

		protected override void DeserializeResult(JToken result, IDictionary<string, string> output)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(output, nameof(output));
			result.AddPropertiesToDictionary(output);
		}
		#endregion
	}
}