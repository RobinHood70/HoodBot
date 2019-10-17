#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropLinks : PropListLinks<LinksInput>, IGeneratorModule
	{
		#region Constructors
		public PropLinks(WikiAbstractionLayer wal, LinksInput input)
			: this(wal, input, null)
		{
		}

		public PropLinks(WikiAbstractionLayer wal, LinksInput input, IPageSetGenerator pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "links";
		#endregion

		#region Protected Override Properties
		protected override string Prefix { get; } = "pl";
		#endregion

		#region Public Static Methods
		public static PropLinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new PropLinks(wal, input as LinksInput, pageSetGenerator);

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

		protected override void GetResultsFromCurrentPage() => this.ResetItems(this.Output?.Links);

		protected override void SetResultsOnCurrentPage() => this.CopyList(this.Output?.Links);
		#endregion
	}
}
