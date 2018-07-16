#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class ListWatchlistRaw : ListModule<WatchlistRawInput, WatchlistRawItem>, IGeneratorModule
	{
		#region Constructors
		public ListWatchlistRaw(WikiAbstractionLayer wal, WatchlistRawInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Static Properties

		// Create a static copy of the module name because it's referred to elsewhere in the code due to an anomaly in the results.
		public static string ModuleName { get; } = "watchlistraw";
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 114;

		public override string Name { get; } = ModuleName;
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "wr";
		#endregion

		#region Public Static Methods
		public static ListWatchlistRaw CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListWatchlistRaw(wal, input as WatchlistRawInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, WatchlistRawInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.Add("namespace", input.Namespaces)
				.AddFlags("prop", input.Properties)
				.AddFilterPiped("show", "changed", input.FilterChanged)
				.AddIfNotNullIf("owner", input.Owner, this.SiteVersion >= 117)
				.AddHiddenIf("token", input.Token, this.SiteVersion >= 117)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override WatchlistRawItem GetItem(JToken result) => result == null
			? null
			: new WatchlistRawItem()
			{
				Namespace = (int?)result["ns"],
				Title = (string)result["title"],
				Changed = result["changed"].AsBCBool(),
			};
		#endregion
	}
}