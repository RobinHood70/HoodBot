namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using static RobinHood70.CommonCode.Globals;

	public class MetaTemplateCreator : PageCreator
	{
		#region Constructors
		public MetaTemplateCreator(params string[] variables) => this.VariableNames = variables == null ? new List<string>() : new List<string>(variables);
		#endregion

		#region Public Properties
		public bool GameSpaceOnly { get; set; } = true;

		public IList<string> VariableNames { get; }
		#endregion

		#region Public Override Methods
		public override Page CreatePage(ISimpleTitle title)
		{
			ThrowNull(title, nameof(title));
			return this.GameSpaceOnly && title.Namespace.Id < 100
				? Default.CreatePage(title)
				: new VariablesPage(title);
		}

		public override PageItem CreatePageItem(int ns, string title, long pageId) => new VariablesPageItem(ns, title, pageId);
		#endregion

		#region Protected Override Methods
		protected override void AddCustomPropertyInputs(IList<IPropertyInput> propertyInputs)
		{
			ThrowNull(propertyInputs, nameof(propertyInputs));
			var variablesInput = new VariablesInput() { Variables = this.VariableNames };
			propertyInputs.Add(variablesInput);
		}
		#endregion
	}
}