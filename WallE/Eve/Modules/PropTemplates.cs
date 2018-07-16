#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.WikiCommon.Globals;

	internal class PropTemplates : PropListLinks<TemplatesInput>, IGeneratorModule
	{
		#region Constructors
		public PropTemplates(WikiAbstractionLayer wal, TemplatesInput input)
			: base(wal, input)
		{
		}
		#endregion

		#region Public Override Properties
		public override int MinimumVersion { get; } = 111;

		public override string Name { get; } = "templates";
		#endregion

		#region Protected Override Properties
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

		protected override void GetResultsFromCurrentPage() => this.ResetItems(this.Output.Templates);

		protected override void SetResultsOnCurrentPage() => this.Output.Templates = this.Items;
		#endregion
	}
}