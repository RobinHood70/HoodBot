namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionRollback : ActionModule<RollbackInput, RollbackResult>
	{
		#region Constructors
		public ActionRollback(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "rollback";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RollbackInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.AddIfNotNull("title", input.Title)
				.AddIfPositiveIf("pageid", input.PageId, this.SiteVersion >= 124)
				.AddIf("tags", input.Tags, this.SiteVersion >= 127)
				.Add("user", input.User)
				.AddIfNotNull("summary", input.Summary)
				.Add("markbot", input.MarkBot)
				.AddIfPositiveIf("watchlist", input.Watchlist, this.SiteVersion >= 117)
				.AddHidden("token", input.Token);
		}

		protected override RollbackResult DeserializeResult(JToken? result)
		{
			result.ThrowNull();
			var title = result.MustHaveString("title");
			return new RollbackResult(
				ns: this.FindRequiredNamespace(title),
				title: title,
				pageId: (long)result.MustHave("pageid"),
				summary: result.MustHaveString("summary"),
				revisionId: (long)result.MustHave("revid"),
				oldRevisionId: (long)result.MustHave("old_revid"),
				lastRevisionId: (long)result.MustHave("last_revid"));
		}
		#endregion
	}
}
