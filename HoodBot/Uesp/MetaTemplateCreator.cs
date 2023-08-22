namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	public class MetaTemplateCreator : PageCreator
	{
		#region Constructors
		public MetaTemplateCreator(PageCreator fallbackCreator, params string[] variables)
		{
			this.FallbackCreator = fallbackCreator;
			this.VariableNames = variables == null
				? new List<string>()
				: new List<string>(variables);
		}
		#endregion

		#region Public Properties
		public PageCreator FallbackCreator { get; }

		public bool GameSpaceOnly { get; init; } = true;

		public IList<string> VariableNames { get; }
		#endregion

		#region Public Override Methods

		/// <inheritdoc/>
		public override Page CreatePage(Title title, PageLoadOptions options, IApiTitle? apiItem) => this.GameSpaceOnly && title.Namespace.Id < 100
			? this.FallbackCreator.CreatePage(title, options, apiItem)
			: new VariablesPage(title, options, apiItem);

		public override PageItem CreatePageItem(int ns, string title, long pageId) => new VariablesPageItem(ns, title, pageId);
		#endregion

		#region Protected Override Methods
		protected override void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs)
		{
			VariablesInput variablesInput = new() { Variables = this.VariableNames };
			propertyInputs.NotNull().Add(variablesInput);
		}
		#endregion
	}
}