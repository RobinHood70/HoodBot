#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using Base;
	using RequestBuilder;
	using WikiCommon;
	using static WikiCommon.Globals;

	internal class PropTemplates : PropLinksBase<TemplatesInput>, IGeneratorModule
	{
		#region Constructors
		public PropTemplates(WikiAbstractionLayer wal, TemplatesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Protected Internal Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "templates";
		#endregion

		#region Public Override Properties
		protected override string Prefix { get; } = "tl";
		#endregion

		#region Public Static Methods
		public static PropTemplates CreateInstance(WikiAbstractionLayer wal, IGeneratorInput input) => new PropTemplates(wal, input as TemplatesInput);

		public static PropTemplates CreateInstance(WikiAbstractionLayer wal, IPropertyInput input) => new PropTemplates(wal, input as TemplatesInput);
		#endregion

		#region Protected Override Methods
		protected override void BuildRequestLocal(Request request, TemplatesInput input)
		{
			ThrowNull(request, nameof(request));
			ThrowNull(input, nameof(input));
			request.Add("templates", input.Templates);
			base.BuildRequestLocal(request, input);
		}

		protected override void GetResultsFromCurrentPage() => this.ResetMyList(this.Output.Templates);

		protected override void SetResultsOnCurrentPage() => this.Output.Templates = this.MyList.AsNewReadOnlyList();
		#endregion
	}
}