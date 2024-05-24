namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class MetaTokens(WikiAbstractionLayer wal, TokensInput input) : QueryModule<TokensInput, IReadOnlyDictionary<string, string>>(wal, input, null)
	{
		#region Public Override Properties
		public override int MinimumVersion => 124;

		public override string Name => "tokens";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType => "meta";

		protected override string Prefix => string.Empty;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TokensInput input)
		{
			ArgumentNullException.ThrowIfNull(request);
			ArgumentNullException.ThrowIfNull(input);
			request.Add("type", input.Types);
		}

		protected override void DeserializeResult(JToken? result)
		{
			ArgumentNullException.ThrowIfNull(result);
			this.Output = result.GetStringDictionary<string>();
		}
		#endregion
	}
}