namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	// IMPNOTE: Result is slightly reformatted from the API to provide a straight-forward collection of pages that were moved.
	internal sealed class ActionMove : ActionModule<MoveInput, IReadOnlyList<MoveItem>>
	{
		#region Constructors
		public ActionMove(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 112;

		public override string Name => "move";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType => RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, MoveInput input)
		{
			input.ThrowNull();
			request
				.NotNull()
				.AddIfNotNull("from", input.From)
				.AddIfPositive("fromid", input.FromId)
				.AddIfNotNull("to", input.To)
				.AddIfNotNull("reason", input.Reason)
				.Add("movetalk", input.MoveTalk)
				.Add("movesubpages", input.MoveSubpages)
				.Add("noredirect", input.NoRedirect)
				.Add("watch", input.Watchlist == WatchlistOption.Watch && this.SiteVersion < 117)
				.Add("unwatch", input.Watchlist == WatchlistOption.Unwatch && this.SiteVersion < 117)
				.AddIfPositiveIf("watchlist", input.Watchlist, this.SiteVersion >= 117)
				.Add("ignorewarnings", input.IgnoreWarnings)
				.AddHidden("token", input.Token);
		}

		protected override IReadOnlyList<MoveItem> DeserializeResult(JToken? result)
		{
			// Errors occur at multiple levels during a move operation and can represent partial success, so instead of throwing them, we gather them into the result and let the user figure out what to do.
			result.ThrowNull();
			List<MoveItem> list = new();
			DeserializeMove(result, list, string.Empty);
			DeserializeMove(result, list, "talk");
			DeserializeSubpages(result["subpages"], list);
			DeserializeSubpages(result["subpages-talk"], list);

			return list;
		}
		#endregion

		#region Private Static Methods
		private static void DeserializeMove(JToken result, IList<MoveItem> output, string prefix)
		{
			if (result[prefix + "from"] != null)
			{
				output.Add(new MoveItem(
					error: result["error"].GetError() ?? result.GetError("talkmove-error-code", "talkmove-error-info"),
					from: (string?)result[prefix + "from"],
					movedOverRedirect: result[prefix + "moveoverredirect"].GetBCBool(),
					redirectCreated: result["redirectcreated"].GetBCBool(),
					to: (string?)result[prefix + "to"]));
			}
		}

		private static void DeserializeSubpages(JToken? node, IList<MoveItem> output)
		{
			if (node != null)
			{
				foreach (var subnode in node)
				{
					DeserializeMove(subnode, output, string.Empty);
				}
			}
		}
		#endregion
	}
}
