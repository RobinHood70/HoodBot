namespace RobinHood70.Testing.MetaTemplate
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;

	public class VariablesPage : Page
	{
		#region Constructors
		public VariablesPage(ISimpleTitle simpleTitle)
			: base(simpleTitle)
		{
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, VariablesResult> VariableSets { get; private set; }
		#endregion

		protected override void PopulateCustomResults(PageItem pageItem)
		{
			var varPageItem = pageItem as VariablesPageItem;
			var dictionary = new Dictionary<string, VariablesResult>();
			foreach (var item in varPageItem.Variables)
			{
				dictionary[item.Subset ?? string.Empty] = item;
			}

			this.VariableSets = new ReadOnlyDictionary<string, VariablesResult>(dictionary);
		}
	}
}