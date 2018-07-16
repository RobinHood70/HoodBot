#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionRollback : ActionModule<RollbackInput, RollbackResult>
	{
		#region Constructors
		public ActionRollback(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "rollback";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, RollbackInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("title", input.Title)
				.AddIfPositiveIf("pageid", input.PageId, this.SiteVersion >= 124)
				.AddIf("tags", input.Tags, this.SiteVersion >= 127)
				.Add("user", input.User)
				.AddIfNotNull("summary", input.Summary)
				.Add("markbot", input.MarkBot)
				.AddIfPositiveIf("watchlist", input.Watchlist, this.SiteVersion >= 117)
				.AddHidden("token", input.Token);
		}

		protected override RollbackResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new RollbackResult()
			{
				Title = (string)result["title"],
				PageId = (long)result["pageid"],
				Summary = (string)result["summary"],
				RevisionId = (long)result["revid"],
				OldRevisionId = (long)result["old_revid"],
				LastRevisionId = (long)result["last_revid"],
			};
			return output;
		}
		#endregion
	}
}
