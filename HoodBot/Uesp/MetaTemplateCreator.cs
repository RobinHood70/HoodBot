namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using static RobinHood70.WikiCommon.Globals;

	public class MetaTemplateCreator : PageCreator
	{
		#region Public Override Methods
		public override Page CreatePage(ISimpleTitle simpleTitle) => new VariablesPage(simpleTitle);

		public override PageItem CreatePageItem() => new VariablesPageItem();
		#endregion

		#region Protected Override Methods
		protected override void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs)
		{
			ThrowNull(propertyInputs, nameof(propertyInputs));
			propertyInputs.Add(new VariablesInput());
		}
		#endregion
	}
}