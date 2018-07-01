#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Newtonsoft.Json.Linq;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropLinksHere : PropListModule<LinksHereInput, LinksHereItem>, IGeneratorModule
	{
		#region Constructors
		public PropLinksHere(WikiAbstractionLayer wal, LinksHereInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 124;

		public override string Name { get; } = "linkshere";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "lh";
		#endregion

		#region Public Static Methods
		public static PropLinksHere CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropLinksHere(wal, input as LinksHereInput);

		public static PropLinksHere CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropLinksHere(wal, input as LinksHereInput);
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

		protected override LinksHereItem GetItem(JToken result)
		{
			if (result == null)
			{
				return null;
			}

			var item = new LinksHereItem();
			item.GetWikiTitle(result);
			item.Redirect = result["redirect"].AsBCBool();

			return item;
		}

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.LinksHere);

		protected override void SetResultsOnCurrentPage() => this.Output.LinksHere = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}