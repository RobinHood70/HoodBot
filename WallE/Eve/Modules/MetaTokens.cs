#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	internal class MetaTokens : QueryModule<TokensInput, IDictionary<string, string>>
	{
		#region Constructors
		public MetaTokens(WikiAbstractionLayer wal, TokensInput input)
			: base(wal, input, new Dictionary<string, string>())
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 120;

		public override string Name { get; } = "tokens";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = string.Empty;

		protected override string ModuleType { get; } = "meta";
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
#pragma warning disable IDE0007 // Use implicit type
			foreach (JProperty token in result)
#pragma warning restore IDE0007 // Use implicit type
			{
				output.Add(token.Name, (string)token.Value);
			}
		}
		#endregion
	}
}