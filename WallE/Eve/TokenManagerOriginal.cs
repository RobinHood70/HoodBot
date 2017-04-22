namespace RobinHood70.WallE.Eve
{
	using System.Collections.Generic;
	using Base;
	using Modules;
	using static TokensInput;

	internal class TokenManagerOriginal : ITokenManager
	{
		#region Constructors
		public TokenManagerOriginal(WikiAbstractionLayer wal) => this.Wal = wal;
		#endregion

		#region Protected Static Properties
		protected static HashSet<string> ValidTypes { get; } = new HashSet<string>
		{
			Edit,
			Patrol,
			Watch,
		};
		#endregion

		#region Protected Fields
		protected Dictionary<string, string> SessionTokens { get; } = new Dictionary<string, string>(ValidTypes.Count);

		protected WikiAbstractionLayer Wal { get; }
		#endregion

		#region Public Methods
		public void Clear() => this.SessionTokens.Clear();

		public string RollbackToken(long pageId) => this.GetToken(PageSetInput.FromPageIds(new long[] { pageId }));

		public string RollbackToken(string title) => this.GetToken(new PageSetInput(new string[] { title }));

		public virtual string SessionToken(string type)
		{
			type = TokenManagerFunctions.ValidateTokenType(ValidTypes, type, Csrf, Edit);
			if (!this.SessionTokens.TryGetValue(type, out string retval))
			{
				var pageSetInput = new PageSetInput(new string[] { ":" }); // This is a hack that MW sees as a legitimate page name that will never actually be found. Fixed in later versions, but works nicely for pre-1.20 since we don't have an actual page name.
				var propInfoInput = new InfoInput { Tokens = new[] { Edit, Watch } };
				var input = new RecentChangesInput { GetPatrolToken = true, MaxItems = 1 };
				var recentChanges = new ListRecentChanges(this.Wal, input);
				var queryInput = new QueryInput(this.Wal, pageSetInput, new[] { propInfoInput }, new[] { recentChanges });
				var pageSet = new ActionQuery(this.Wal, WikiAbstractionLayer.DefaultPageFactory).SubmitPageSet(queryInput);
				var rc = recentChanges.Output;

				foreach (var page in pageSet)
				{
					foreach (var token in page.Value.Info.Tokens)
					{
						this.SessionTokens[TokenManagerFunctions.TrimToken(token.Key)] = token.Value;
					}

					break;
				}

				if (rc.Count > 0)
				{
					this.SessionTokens[Patrol] = rc[0].PatrolToken;
				}

				this.SessionTokens.TryGetValue(type, out retval);
			}

			return retval;
		}

		public string UserRightsToken(string user)
		{
			var usersInput = new UsersInput(new string[] { user })
			{
				GetRightsToken = true,
			};
			var users = this.Wal.Users(usersInput);
			if (users.Count == 1)
			{
				return users[0].Token;
			}

			return null;
		}
		#endregion

		#region Private Methods
		private string GetToken(PageSetInput pageSetInput)
		{
			var revisions = new RevisionsInput() { GetRollbackToken = true };
			var pages = this.Wal.LoadPages(pageSetInput, new IPropertyInput[] { revisions });
			foreach (var page in pages)
			{
				foreach (var revision in page.Value.Revisions)
				{
					return revision.RollbackToken;
				}
			}

			return null;
		}
		#endregion
	}
}