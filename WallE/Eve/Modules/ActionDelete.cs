#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static WikiCommon.Globals;

	public class ActionDelete : ActionModule<DeleteInput, DeleteResult>
	{
		#region Constructors
		public ActionDelete(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "delete";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Public Override Methods
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

		protected override DeleteResult DeserializeResult(JToken result)
		{
			ThrowNull(result, nameof(result));
			var output = new DeleteResult()
			{
				LogId = (long?)result["logid"] ?? 0,
				Reason = (string)result["reason"],
				Title = (string)result["title"],
			};
			return output;
		}
		#endregion
	}
}
