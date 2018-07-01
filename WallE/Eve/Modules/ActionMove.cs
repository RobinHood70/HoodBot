#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	// IMPNOTE: Result is slightly reformatted from the API to provide a straight-forward collection of pages that were moved.
	public class ActionMove : ActionModule<MoveInput, MoveResult>
	{
		#region Constructors
		public ActionMove(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 112;

		public override string Name { get; } = "move";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, MoveInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
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

		protected override MoveResult DeserializeResult(JToken result)
		{
			// Errors occur at multiple levels during a move operation and can represent partial success, so instead of throwing them, we gather them into the result and let the user figure out what to do.
			ThrowNull(result, nameof(result));
			var list = new List<MoveItem>();
			DeserializeMove(result, list, string.Empty);
			DeserializeMove(result, list, "talk");
			DeserializeSubpages(result["subpages"], list);
			DeserializeSubpages(result["subpages-talk"], list);

			var output = new MoveResult(list)
			{
				Reason = (string)result["reason"],
				RedirectCreated = result["redirectcreated"].AsBCBool(),
			};
			return output;
		}
		#endregion

		#region Private Methods
		private static void DeserializeMove(JToken result, IList<MoveItem> output, string prefix)
		{
			if (result[prefix + "from"] != null)
			{
				var item = new MoveItem()
				{
					Error = result["error"].GetError() ?? result.GetError("talkmove-error-code", "talkmove-error-info"),
					From = (string)result[prefix + "from"],
					To = (string)result[prefix + "to"],
					MovedOverRedirect = result[prefix + "moveoverredirect"].AsBCBool(),
				};
				output.Add(item);
			}
		}

		private static void DeserializeSubpages(JToken node, IList<MoveItem> output)
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
