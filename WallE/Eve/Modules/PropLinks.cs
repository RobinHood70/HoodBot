namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class PropLinks : PropListLinks<LinksInput>, IGeneratorModule
	{
		#region Constructors
		public PropLinks(WikiAbstractionLayer wal, LinksInput input)
			: this(wal, input, null)
		{
		}

		public PropLinks(WikiAbstractionLayer wal, LinksInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "links";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "pl";
		#endregion

		#region Public Static Methods
		public static PropLinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (LinksInput)input, pageSetGenerator);

		public static PropLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (LinksInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LinksInput input)
		{
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request)).Add("titles", input.Titles);
			base.BuildRequestLocal(request, input);
		}

		protected override ICollection<IApiTitle> GetMutableList(PageItem page) => (ICollection<IApiTitle>)page.Links;
		#endregion
	}
}
