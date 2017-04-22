﻿namespace RobinHood70.WallE.Eve
{
	using System.Collections.Generic;
	using Modules;
	using static TokensInput;

	internal class TokenManagerMeta : ITokenManager
	{
		#region Public Constants
		public const int MinimumVersion = 124;
		#endregion

		#region Fields

		// Can't be static in this version, since Login will be removed for MW 1.24-1.27
		private HashSet<string> validTypes = new HashSet<string>
		{
			Csrf,
			Login,
			Patrol,
			Rollback,
			UserRights,
			Watch,
		};

		private Dictionary<string, string> sessionTokens = new Dictionary<string, string>(6);
		private WikiAbstractionLayer wal;
		#endregion

		#region Constructors
		public TokenManagerMeta(WikiAbstractionLayer wal)
		{
			this.wal = wal;
			if (wal.SiteVersion < 128)
			{
				this.validTypes.Remove(Login);
			}
		}
		#endregion

		#region Public Override Methods
		public string RollbackToken(long pageId) => this.AnyToken(Rollback);

		public string RollbackToken(string title) => this.AnyToken(Rollback);

		public string SessionToken(string type) => this.AnyToken(TokenManagerFunctions.ValidateTokenType(this.validTypes, type, Edit, Csrf));

		public string UserRightsToken(string user) => this.AnyToken(UserRights);
		#endregion

		#region Public Methods
		public void Clear() => this.sessionTokens.Clear();
		#endregion

		#region Private Methods
		private string AnyToken(string type)
		{
			if (!this.sessionTokens.TryGetValue(type, out string retval))
			{
				// Ask for all session tokens unless a login token has been requested.
				var tokensInput = type == Login ? new TokensInput(new string[] { type }) : new TokensInput(this.validTypes);
				var tokensModule = new MetaTokens(this.wal, tokensInput);
				var query = new ActionQuery(this.wal);
				var queryInput = new QueryInput(tokensModule);
				query.Submit(queryInput);

				var tokens = tokensModule.Output;
				foreach (var token in tokens)
				{
					this.sessionTokens[TokenManagerFunctions.TrimToken(token.Key)] = token.Value;
				}

				this.sessionTokens.TryGetValue(type, out retval);
			}

			return retval;
		}
		#endregion
	}
}