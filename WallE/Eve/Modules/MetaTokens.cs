namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class MetaTokens : QueryModule<TokensInput, IReadOnlyDictionary<string, string>>
	{
		#region Constructors
		public MetaTokens(WikiAbstractionLayer wal, TokensInput input)
			: base(wal, input, null)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 124;

		public override string Name => "tokens";
		#endregion

		#region Protected Override Properties
		protected override string ModuleType => "meta";

		protected override string Prefix => string.Empty;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TokensInput input) => request
			.NotNull()
			.Add("type", input.NotNull().Types);

		protected override void DeserializeResult(JToken? result) => this.Output = result
			.NotNull()
			.GetStringDictionary<string>();
		#endregion
	}
}