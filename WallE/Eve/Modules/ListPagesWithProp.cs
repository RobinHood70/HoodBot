#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal class ListPagesWithProp : ListModule<PagesWithPropertyInput, PagesWithPropertyItem>, IGeneratorModule
	{
		#region Constructors
		public ListPagesWithProp(WikiAbstractionLayer wal, PagesWithPropertyInput input)
			: this(wal, input, null)
		{
		}

		public ListPagesWithProp(WikiAbstractionLayer wal, PagesWithPropertyInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 121;

		public override string Name => "pageswithprop";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "pwp";
		#endregion

		#region Public Static Methods
		public static ListPagesWithProp CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is PagesWithPropertyInput listInput
				? new ListPagesWithProp(wal, listInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(PagesWithPropertyInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, PagesWithPropertyInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddIfNotNull("propname", input.PropertyName)
				.AddFlags("prop", input.Properties)
				.AddIf("dir", "descending", input.SortDescending)
				.Add("limit", this.Limit);
		}

		protected override PagesWithPropertyItem? GetItem(JToken result) => result == null
			? null
			: new PagesWithPropertyItem(
				ns: (int?)result["ns"],
				title: (string?)result["title"],
				pageId: (long?)result["pageid"] ?? 0,
				value: (string?)result["value"]);
		#endregion
	}
}
