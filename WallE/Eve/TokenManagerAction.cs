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
		public override string SessionToken(string type)
		{
			type = TokenManagerFunctions.ValidateTokenType(ValidTypes, type, Csrf, Edit);
			if (!this.SessionTokens.TryGetValue(type, out string retval))
			{
				var action = new ActionTokens(this.Wal);
				var tokensInput = new TokensInput(ValidTypes);
				var tokens = action.Submit(tokensInput);
				foreach (var token in tokens)
				{
					this.SessionTokens[TokenManagerFunctions.TrimToken(token.Key)] = token.Value;
				}

				this.SessionTokens.TryGetValue(type, out retval);
			}

			return retval;
		}
		#endregion
	}
}