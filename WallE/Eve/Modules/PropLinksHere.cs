namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal sealed class PropLinksHere(WikiAbstractionLayer wal, LinksHereInput input, IPageSetGenerator? pageSetGenerator) : PropListModule<LinksHereInput, LinksHereResult, LinksHereItem>(wal, input, pageSetGenerator), IGeneratorModule
	{
		#region Constructors
		public PropLinksHere(WikiAbstractionLayer wal, LinksHereInput input)
			: this(wal, input, null)
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
		public static PropLinksHere CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (LinksHereInput)input, pageSetGenerator);

		public static PropLinksHere CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (LinksHereInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LinksHereInput input)
		{
			ArgumentNullException.ThrowIfNull(input);
			ArgumentNullException.ThrowIfNull(request);
			request
				.AddFlags("prop", input.Properties)
				.Add("namespace", input.Namespaces)
				.AddFilterPiped("show", "redirect", input.FilterRedirects)
				.Add("limit", this.Limit);
		}

		protected override LinksHereItem? GetItem(JToken result) => result == null
			? null
			: new LinksHereItem(
				ns: (int?)result["ns"],
				title: (string?)result["title"],
				pageId: (long?)result["pageid"] ?? 0,
				redirect: result["redirect"].GetBCBool());

		protected override LinksHereResult GetNewList(JToken parent) => [];
		#endregion
	}
}