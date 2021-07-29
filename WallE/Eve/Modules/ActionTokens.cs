﻿namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionTokens : ActionModule<TokensInput, IReadOnlyDictionary<string, string>>
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
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request)).Add("type", input.Types);
		}

		protected override IReadOnlyDictionary<string, string> DeserializeResult(JToken? result)
		{
			result.ThrowNull(nameof(result));
			return result.GetStringDictionary<string>();
		}
		#endregion
	}
}