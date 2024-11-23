namespace RobinHood70.WallE.Eve;

using System;
using System.Collections.Generic;
using System.Linq;
using RobinHood70.WallE.Base;
using RobinHood70.WallE.Eve.Modules;
using static RobinHood70.WallE.Eve.TokensInput;

internal sealed class TokenManagerMeta(WikiAbstractionLayer wal) : ITokenManager
{
	#region Public Constants
	public const int MinimumVersion = 124;
	public const int MinimumVersionLogin = 127;
	#endregion

	#region Fields
	private static readonly HashSet<string> ValidTypes = new(StringComparer.Ordinal)
	{
		Csrf,
		Patrol,
		Rollback,
		UserRights,
		Watch,
	};

	private readonly Dictionary<string, string> sessionTokens = new(6, StringComparer.Ordinal);
	#endregion

	#region Public Override Methods
	public string? LoginToken()
	{
		if (wal.SiteVersion >= MinimumVersionLogin)
		{
			var tokens = wal.RunModuleQuery(new MetaTokens(wal, new TokensInput(Login)));
			return tokens.Count == 0 ? null : tokens.First().Value;
		}

		return null;
	}

	public string? RollbackToken(long pageId) => this.AnyToken(Rollback);

	public string? RollbackToken(string title) => this.AnyToken(Rollback);

	public string? SessionToken(string type) => this.AnyToken(TokenManagerFunctions.ValidateTokenType(ValidTypes, type, Edit, Csrf));

	public string? UserRightsToken(string userName) => this.AnyToken(UserRights);
	#endregion

	#region Public Methods
	public void Clear() => this.sessionTokens.Clear();
	#endregion

	#region Private Methods
	private string? AnyToken(string type)
	{
		if (this.sessionTokens.Count == 0)
		{
			var tokens = wal.RunModuleQuery(new MetaTokens(wal, new TokensInput(ValidTypes)));
			foreach (var token in tokens)
			{
				this.sessionTokens[TokenManagerFunctions.TrimTokenKey(token.Key)] = token.Value;
			}
		}

		this.sessionTokens.TryGetValue(type, out var retval);
		return retval;
	}
	#endregion
}