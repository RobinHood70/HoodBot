#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	internal class PropLinks : PropLinksBase<LinksInput>, IGeneratorModule
	{
		#region Constructors
		public PropLinks(WikiAbstractionLayer wal, LinksInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "links";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "pl";
		#endregion

		#region Public Static Methods
		public static PropLinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropLinks(wal, input as LinksInput);

		public static PropLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropLinks(wal, input as LinksInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("titles", input.Titles);
			base.BuildRequestLocal(request, input);
		}

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.Links);

		protected override void SetResultsOnCurrentPage() => this.Output.Links = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}
