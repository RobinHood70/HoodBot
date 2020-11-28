﻿namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ActionDelete : ActionModule<DeleteInput, DeleteResult>
	{
		#region Constructors
		public ActionDelete(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "delete";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, DeleteInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("title", input.Title)
				.AddIfPositive("pageid", input.PageId)
				.AddIfNotNull("reason", input.Reason)
				.Add("tags", input.Tags)
				.Add("watch", input.Watchlist == WatchlistOption.Watch && this.SiteVersion < 117)
				.Add("unwatch", input.Watchlist == WatchlistOption.Unwatch && this.SiteVersion < 117)
				.AddIfPositiveIf("watchlist", input.Watchlist, this.SiteVersion >= 117)
				.AddIfNotNull("oldimage", input.OldImage)
				.AddHidden("token", input.Token);
		}

		protected override DeleteResult DeserializeResult(JToken? result)
		{
			ThrowNull(result, nameof(result));
			return new DeleteResult(
				title: result.MustHaveString("title"),
				reason: result.MustHaveString("reason"),
				logId: (long?)result["logid"] ?? 0);
		}
		#endregion
	}
}
