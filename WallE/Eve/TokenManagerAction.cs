namespace RobinHood70.WallE.Eve
{
	using RobinHood70.WallE.Eve.Modules;
	using static RobinHood70.WallE.Eve.TokensInput;

	internal class TokenManagerAction : TokenManagerOriginal
	{
		#region Public Constants
		public const int MinimumVersion = 120;
		#endregion

		#region Constructors
		public TokenManagerAction(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Methods
		public override string? SessionToken(string type)
		{
			type = TokenManagerFunctions.ValidateTokenType(ValidTypes, type, Csrf, Edit);
			if (this.SessionTokens.Count == 0)
			{
				var action = new ActionTokens(this.Wal);
				var tokensInput = new TokensInput(ValidTypes);
				var tokens = action.Submit(tokensInput);
				foreach (var token in tokens)
				{
					this.SessionTokens[TokenManagerFunctions.TrimTokenKey(token.Key)] = token.Value;
				}
			}

			this.SessionTokens.TryGetValue(type, out var retval);
			return retval;
		}
		#endregion
	}
}