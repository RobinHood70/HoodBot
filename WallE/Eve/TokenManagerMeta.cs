namespace RobinHood70.WallE.Eve
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.WallE.Eve.Modules;
	using static RobinHood70.WallE.Eve.TokensInput;

	internal sealed class TokenManagerMeta : ITokenManager
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
		private readonly WikiAbstractionLayer wal;
		#endregion

		#region Constructors
		public TokenManagerMeta(WikiAbstractionLayer wal) => this.wal = wal;
		#endregion

		#region Public Override Methods
		public string? LoginToken()
		{
			if (this.wal.SiteVersion >= MinimumVersionLogin)
			{
				var tokens = this.wal.RunModuleQuery(new MetaTokens(this.wal, new TokensInput(Login)));
				foreach (var token in tokens)
				{
					return token.Value;
				}
			}

			return null;
		}

		public string? RollbackToken(long pageId) => this.AnyToken(Rollback);

		public string? RollbackToken(string title) => this.AnyToken(Rollback);

		public string? SessionToken(string type) => this.AnyToken(TokenManagerFunctions.ValidateTokenType(ValidTypes, type, Edit, Csrf));

		public string? UserRightsToken(string user) => this.AnyToken(UserRights);
		#endregion

		#region Public Methods
		public void Clear() => this.sessionTokens.Clear();
		#endregion

		#region Private Methods
		private string? AnyToken(string type)
		{
			if (this.sessionTokens.Count == 0)
			{
				var tokens = this.wal.RunModuleQuery(new MetaTokens(this.wal, new TokensInput(ValidTypes)));
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
}