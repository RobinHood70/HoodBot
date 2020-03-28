#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal class MetaTokens : QueryModule<TokensInput, IReadOnlyDictionary<string, string>>
	{
		#region Constructors
		public MetaTokens(WikiAbstractionLayer wal, TokensInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 120;

		public override string Name => "tokens";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType => "meta";

		protected override string Prefix => string.Empty;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TokensInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("type", input.Types);
		}

		protected override void DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			this.Output = result.GetStringDictionary<string>();
		}
		#endregion
	}
}