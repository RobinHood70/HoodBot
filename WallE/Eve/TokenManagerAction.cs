namespace RobinHood70.WallE.Eve;

using RobinHood70.WallE.Eve.Modules;
using static RobinHood70.WallE.Eve.TokensInput;

internal sealed class TokenManagerAction(WikiAbstractionLayer wal) : TokenManagerOriginal(wal)
{
	#region Public Constants
	public const int MinimumVersion = 120;
	#endregion

	#region Public Override Methods
	public override string? SessionToken(string type)
	{
		type = TokenManagerFunctions.ValidateTokenType(ValidTypes, type, Csrf, Edit);
		if (this.SessionTokens.Count == 0)
		{
			ActionTokens action = new(this.Wal);
			TokensInput tokensInput = new(ValidTypes);
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