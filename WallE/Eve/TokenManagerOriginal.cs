namespace RobinHood70.WallE.Eve
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Eve.Modules;

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

		public string? LoginToken() => null;

		public string? RollbackToken(long pageId) => this.GetRollbackToken(QueryPageSetInput.FromPageIds(new[] { pageId }));

		public string? RollbackToken(string title) => this.GetRollbackToken(new QueryPageSetInput(new[] { title }));

		public virtual string? SessionToken(string type)
		{
			type = TokenManagerFunctions.ValidateTokenType(ValidTypes, type, TokensInput.Csrf, TokensInput.Edit);
			if (this.SessionTokens.Count == 0)
			{
				var pageSetInput = new QueryPageSetInput(DummyPage);
				var propInfoInput = new InfoInput { Tokens = new[] { TokensInput.Edit, TokensInput.Watch } };
				var input = new RecentChangesInput { GetPatrolToken = true, MaxItems = 1 };
				var recentChanges = new ListRecentChanges(this.Wal, input);
				var propertyModules = this.Wal.ModuleFactory.CreateModules(new[] { propInfoInput });
				var queryInput = new QueryInput(pageSetInput, propertyModules, new[] { recentChanges });
				var pageSet = this.Wal.RunPageSetQuery(queryInput, WikiAbstractionLayer.DefaultPageFactory);
				if (pageSet.Count == 1 && pageSet[0].Info is PageInfo info)
				{
					foreach (var token in info.Tokens)
					{
						this.SessionTokens[TokenManagerFunctions.TrimTokenKey(token.Key)] = token.Value;
					}
				}

				if (recentChanges.Output is IList<RecentChangesItem> rc && rc.Count > 0 && rc[0].PatrolToken is string patrolToken)
				{
					this.SessionTokens[TokensInput.Patrol] = patrolToken;
				}
			}

			this.SessionTokens.TryGetValue(type, out var retval);
			return retval;
		}

		public string? UserRightsToken(string user)
		{
			var usersInput = new UsersInput(new[] { user })
			{
				GetRightsToken = true,
			};
			var users = this.Wal.Users(usersInput);
			return users.Count == 1 ? users[0].Token : null;
		}
		#endregion

		#region Private Methods
		private string? GetRollbackToken(QueryPageSetInput pageSetInput)
		{
			var revisions = new RevisionsInput() { GetRollbackToken = true };
			var pages = this.Wal.LoadPages(pageSetInput, new[] { revisions });

			// By all rights, there should only be one page and one revision in the collection, but we don't really care if something very weird happens here, just as long as at least ONE of them has a rollback token, so just iterate the entire set if somehow there are multiple results.
			foreach (var page in pages)
			{
				foreach (var rev in page.Revisions)
				{
					if (rev.RollbackToken != null)
					{
						return rev.RollbackToken;
					}
				}
			}

			return null;
		}
		#endregion
	}
}