namespace RobinHood70.WallE.Eve
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Eve.Modules;
	using RobinHood70.WallE.Properties;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	internal class TokenManagerOriginal : ITokenManager
	{
		#region Static Fields
		private static readonly string[] DummyPage = { ":" }; // This is a hack that MW sees as a legitimate page name that will never actually be found. Fixed in later versions, but works nicely for pre-1.20 since we don't have an actual page name.
		#endregion

		#region Constructors
		public TokenManagerOriginal(WikiAbstractionLayer wal) => this.Wal = wal;
		#endregion

		#region Protected Static Properties
		protected static HashSet<string> ValidTypes { get; } = new HashSet<string>
		{
			TokensInput.Edit,
			TokensInput.Patrol,
			TokensInput.Watch,
		};
		#endregion

		#region Protected Fields
		protected Dictionary<string, string> SessionTokens { get; } = new Dictionary<string, string>(ValidTypes.Count);

		protected WikiAbstractionLayer Wal { get; }
		#endregion

		#region Public Methods
		public void Clear() => this.SessionTokens.Clear();

		public string RollbackToken(long pageId) => this.GetRollbackToken(QueryPageSetInput.FromPageIds(new[] { pageId }));

		public string RollbackToken(string title) => this.GetRollbackToken(new QueryPageSetInput(new[] { title }));

		public virtual string SessionToken(string type)
		{
			type = TokenManagerFunctions.ValidateTokenType(ValidTypes, type, TokensInput.Csrf, TokensInput.Edit);
			if (!this.SessionTokens.TryGetValue(type, out var retval))
			{
				var pageSetInput = new QueryPageSetInput(DummyPage);
				var propInfoInput = new InfoInput { Tokens = new[] { TokensInput.Edit, TokensInput.Watch } };
				var input = new RecentChangesInput { GetPatrolToken = true, MaxItems = 1 };
				var recentChanges = new ListRecentChanges(this.Wal, input);
				var propertyModules = this.Wal.ModuleFactory.CreateModules(new[] { propInfoInput });
				var queryInput = new QueryInput(pageSetInput, propertyModules, new[] { recentChanges });
				var pageSet = this.Wal.RunPageSetQuery(queryInput);
				var rc = recentChanges.Output;

				foreach (var page in pageSet)
				{
					foreach (var token in page.Value.Info.Tokens)
					{
						this.SessionTokens[TokenManagerFunctions.TrimTokenKey(token.Key)] = token.Value;
					}

					break;
				}

				if (rc.Count > 0)
				{
					this.SessionTokens[TokensInput.Patrol] = rc[0].PatrolToken;
				}

				this.SessionTokens.TryGetValue(type, out retval);
			}

			return retval;
		}

		public string UserRightsToken(string user)
		{
			var usersInput = new UsersInput(new[] { user })
			{
				GetRightsToken = true,
			};
			var users = this.Wal.Users(usersInput);
			return (users.Count == 1 ? users[0].Token : null) ?? throw new WikiException(CurrentCulture(EveMessages.InvalidToken, TokensInput.UserRights));
		}
		#endregion

		#region Private Methods
		private string GetRollbackToken(QueryPageSetInput pageSetInput)
		{
			var revisions = new RevisionsInput() { GetRollbackToken = true };
			var pages = this.Wal.LoadPages(pageSetInput, new[] { revisions });
			return pages.First()?.Revisions.First()?.RollbackToken ?? throw new WikiException(CurrentCulture(EveMessages.InvalidToken, TokensInput.Rollback));
		}
		#endregion
	}
}