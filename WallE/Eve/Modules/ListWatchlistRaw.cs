namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class ListWatchlistRaw(WikiAbstractionLayer wal, WatchlistRawInput input, IPageSetGenerator? pageSetGenerator) : ListModule<WatchlistRawInput, WatchlistRawItem>(wal, input, pageSetGenerator), IGeneratorModule
	{
		#region Constructors
		public ListWatchlistRaw(WikiAbstractionLayer wal, WatchlistRawInput input)
			: this(wal, input, null)
		{
		}
		#endregion

		#region Public Static Properties

		// Create a static copy of the module name because it's referred to elsewhere in the code due to an anomaly in the results.
		public static string ModuleName => "watchlistraw";
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 114;

		public override string Name => ModuleName;
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "wr";
		#endregion

		#region Public Static Methods
		public static ListWatchlistRaw CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (WatchlistRawInput)input, pageSetGenerator);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, WatchlistRawInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
			request
				.Add("namespace", input.Namespaces)
				.AddFlags("prop", input.Properties)
				.AddFilterPiped("show", "changed", input.FilterChanged)
				.AddIfNotNullIf("owner", input.Owner, this.SiteVersion >= 117)
				.AddHiddenIf("token", input.Token, this.SiteVersion >= 117)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override WatchlistRawItem? GetItem(JToken result) => result == null
			? null
			: new WatchlistRawItem(
				ns: (int)result.MustHave("ns"),
				title: result.MustHaveString("title"),
				changed: result["changed"].GetBCBool());
		#endregion
	}
}