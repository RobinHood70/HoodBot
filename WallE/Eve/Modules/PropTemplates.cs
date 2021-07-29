namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.RequestBuilder;

	internal sealed class PropTemplates : PropListLinks<TemplatesInput>, IGeneratorModule
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
		public static PropTemplates CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input, IPageSetGenerator pageSetGenerator) => new(wal, (TemplatesInput)input, pageSetGenerator);

		public static PropTemplates CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new(wal, (TemplatesInput)input);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TemplatesInput input)
		{
			input.ThrowNull(nameof(input));
			request
				.NotNull(nameof(request))
				.Add("templates", input.Templates);
			base.BuildRequestLocal(request, input);
		}

		protected override ICollection<IApiTitle> GetMutableList(PageItem page) => (ICollection<IApiTitle>)page.Templates;
		#endregion
	}
}