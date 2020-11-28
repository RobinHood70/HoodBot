namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class PropLinksHere : PropListModule<LinksHereInput, LinksHereItem>, IGeneratorModule
	{
		#region Constructors
		public PropLinksHere(WikiAbstractionLayer wal, LinksHereInput input)
			: this(wal, input, null)
		{
		}

		public PropLinksHere(WikiAbstractionLayer wal, LinksHereInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 124;

		public override string Name => "linkshere";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "lh";
		#endregion

		#region Public Static Methods
		public static PropLinksHere CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is LinksHereInput propInput
				? new PropLinksHere(wal, propInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(LinksHereInput), input.GetType().Name);

		public static PropLinksHere CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is LinksHereInput propInput
				? new PropLinksHere(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(LinksHereInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LinksHereInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request
				.AddFlags("prop", input.Properties)
				.Add("namespace", input.Namespaces)
				.AddFilterPiped("show", "redirect", input.FilterRedirects)
				.Add("limit", this.Limit);
		}

		protected override LinksHereItem? GetItem(JToken result, PageItem page) => result == null
			? null
			: new LinksHereItem(
				ns: (int?)result["ns"],
				title: (string?)result["title"],
				pageId: (long?)result["pageid"] ?? 0,
				redirect: result["redirect"].GetBCBool());

		protected override ICollection<LinksHereItem> GetMutableList(PageItem page) => (ICollection<LinksHereItem>)page.LinksHere;
		#endregion
	}
}