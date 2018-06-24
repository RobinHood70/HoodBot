﻿namespace RobinHood70.Robby.Tests.MetaTemplate
{
	using System.Collections.Generic;
	using Design;
	using WallE.Base;
	using static WikiCommon.Globals;

	public class MetaTemplateBuilder : PageCreator
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