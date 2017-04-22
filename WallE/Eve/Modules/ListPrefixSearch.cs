#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using Newtonsoft.Json.Linq;
	using RequestBuilder;
	using static RobinHood70.Globals;

	internal class ListPrefixSearch : ListModule<PrefixSearchInput, WikiTitleItem>, IGeneratorModule
	{
		#region Constructors
		public ListPrefixSearch(WikiAbstractionLayer wal, PrefixSearchInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 123;

		public override string Name { get; } = "prefixsearch";
		#endregion

		#region Public Override Properties
		protected override string BasePrefix { get; } = "ps";
		#endregion

		#region Public Static Methods
		public static ListPrefixSearch CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new ListPrefixSearch(wal, input as PrefixSearchInput);
		#endregion

		#region Public Override Methods
		protected override void BuildRequestLocal(Request request, PrefixSearchInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("search", input.Search)
				.Add("namespace", input.Namespaces)
				.Add("limit", this.Limit);
		}

		protected override WikiTitleItem GetItem(JToken result) => result.GetWikiTitle();
		#endregion
	}
}