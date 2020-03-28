#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon.RequestBuilder;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Eve.ParsingExtensions;

	internal class PropTemplates : PropListLinks<TemplatesInput>, IGeneratorModule
	{
		#region Constructors
		public PropTemplates(WikiAbstractionLayer wal, TemplatesInput input)
			: this(wal, input, null)
		{
		}

		public PropTemplates(WikiAbstractionLayer wal, TemplatesInput input, IPageSetGenerator? pageSetGenerator)
			: base(wal, input, pageSetGenerator)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion => 111;

		public override string Name => "templates";
		#endregion

		#region Protected Override Properties
		protected override string Prefix => "tl";
		#endregion

		#region Public Static Methods
		public static PropTemplates CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) =>
			input is TemplatesInput propInput
				? new PropTemplates(wal, propInput, pageSetGenerator)
				: throw InvalidParameterType(nameof(input), nameof(TemplatesInput), input.GetType().Name);

		public static PropTemplates CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) =>
			input is TemplatesInput propInput
				? new PropTemplates(wal, propInput)
				: throw InvalidParameterType(nameof(input), nameof(TemplatesInput), input.GetType().Name);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TemplatesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("templates", input.Templates);
			base.BuildRequestLocal(request, input);
		}

		protected override ICollection<ITitle> GetMutableList(PageItem page) => (ICollection<ITitle>)page.Templates;
		#endregion
	}
}