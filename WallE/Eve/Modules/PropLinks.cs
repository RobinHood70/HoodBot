#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal class PropLinks : PropListLinks<LinksInput>, IGeneratorModule
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
		public static PropLinks CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is LinksInput propInput
				? new PropLinks(wal, propInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(LinksInput), input.GetType().Name);

		public static PropLinks CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is LinksInput propInput
				? new PropLinks(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(LinksInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, LinksInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("titles", input.Titles);
			base.BuildRequestLocal(request, input);
		}

		protected override ICollection<ITitle> GetMutableList(PageItem page) => (ICollection<ITitle>)page.Links;
		#endregion
	}
}
