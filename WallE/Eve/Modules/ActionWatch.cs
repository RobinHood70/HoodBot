#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Diagnostics;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	public class ActionWatch : ActionModulePageSet<WatchInput, WatchItem>
	{
		#region Constructors
		public ActionWatch(WikiAbstractionLayer wal)
			: base(wal)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 114;

		public override string Name { get; } = "watch";
		#endregion

		#region Protected Override Properties
		protected override RequestType RequestType { get; } = RequestType.Post;
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestPageSet(Request request, WatchInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			if (this.SiteVersion < 123)
			{
				Debug.Assert(input.ListType == ListType.Titles && input.Values.Count == 1 && this.Generator == null, "Incorrect values sent to < MW 1.23 Watch");
				request.Remove("titles");
				request.Remove("converttitles");
				request.Remove("redirects");
				request.Add("title", input.Values[0]);
			}

			request
				.Add("unwatch", input.Unwatch)
				.AddHidden("token", input.Token);
		}

		protected override void DeserializePage(JToken result, WatchItem page)
		{
			ThrowNull(result, nameof(result));
			ThrowNull(page, nameof(page));
			page.Flags =
				result.GetFlag("missing", WatchFlags.Missing) |
				result.GetFlag("unwatched", WatchFlags.Unwatched) |
				result.GetFlag("watched", WatchFlags.Watched);
			page.Namespace = this.FindNamespace(page.Title);
			this.Pages.Add(page);
		}
		#endregion

		#region Private Methods
		private int? FindNamespace(string title)
		{
			var nsSplit = title.Split(new[] { ':' }, 2);
			if (nsSplit.Length == 2)
			{
				var nsText = nsSplit[0];
				foreach (var ns in this.Wal.Namespaces)
				{
					if (nsText == ns.Value.Name)
					{
						return ns.Key;
					}
				}
			}

			// No namespaces matched, or title didn't have a colon, so it must be in Main space.
			return 0;
		}
		#endregion
	}
}
