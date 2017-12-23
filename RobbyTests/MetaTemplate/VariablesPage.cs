namespace RobinHood70.Robby.Tests.MetaTemplate
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using Pages;
	using RobinHood70.WallE.Base;

	public class VariablesPage : Page
	{
		#region Constructors
		public VariablesPage(Site site, string fullPageName)
			: base(site, fullPageName)
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