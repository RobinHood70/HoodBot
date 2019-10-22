#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ActionUndelete : ActionModule<UndeleteInput, UndeleteResult>
	{
		#region Constructors
		public ActionUndelete(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "undelete";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, UndeleteInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("title", input.Title)
				.AddIfNotNull("reason", input.Reason)
				.AddIf("tags", input.Tags, this.SiteVersion >= 125)
				.Add("timestamps", input.Timestamps)
				.AddIf("fileids", input.FileIds, this.SiteVersion >= 124)
				.AddIfPositive("watchlist", input.Watchlist)
				.AddHidden("token", input.Token);
		}

		protected override UndeleteResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			return new UndeleteResult(
				title: result.MustHaveString("title"),
				revisions: (int)result.MustHave("revisions"),
				fileVersions: (int)result.MustHave("fileversions"),
				reason: result.MustHaveString("reason"));
		}
		#endregion
	}
}
