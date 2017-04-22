namespace RobinHood70.Robby.Tests.MetaTemplate
{
	using System.Collections.Generic;
	using Design;
	using Pages;
	using WallE.Base;
	using static Globals;

	public class MetaTemplateBuilder : PageBuilderBase
	{
		#region Public Override Methods
		public override Page CreatePage(Site site, int ns, string title, PageLoadOptions options) => new VariablesPage(site, title, options);

		public override PageItem CreatePageItem() => new VariablesPageItem();
		#endregion

		#region Protected Override Methods
		protected override void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs)
		{
			ThrowNull(propertyInputs, nameof(propertyInputs));
			propertyInputs.Add(new VariablesInput());
		}

		protected override void PopulateCustom(Page page, PageItem pageItem)
		{
			var varPage = page as VariablesPage;
			var varPageItem = pageItem as VariablesPageItem;
			var dictionary = varPage.VariableSets as Dictionary<string, VariablesResult>;
			foreach (var item in varPageItem.Variables)
			{
				dictionary[item.Subset ?? string.Empty] = item;
			}
		}
		#endregion
	}
}