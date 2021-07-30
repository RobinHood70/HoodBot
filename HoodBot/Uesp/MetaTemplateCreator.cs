namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;

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
		public override Page CreatePage(ISimpleTitle title) => this.GameSpaceOnly && title.NotNull(nameof(title)).Namespace.Id < 100
				? this.FallbackCreator.CreatePage(title)
				: new VariablesPage(title);

		public override PageItem CreatePageItem(int ns, string title, long pageId) => new VariablesPageItem(ns, title, pageId);
		#endregion

		#region Protected Override Methods
		protected override void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs)
		{
			var variablesInput = new VariablesInput() { Variables = this.VariableNames };
			propertyInputs.NotNull(nameof(propertyInputs)).Add(variablesInput);
		}
		#endregion
	}
}