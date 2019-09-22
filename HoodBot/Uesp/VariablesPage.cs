namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using static RobinHood70.WikiCommon.Globals;

	public class VariablesPage : Page
	{
		#region Constructors
		public VariablesPage(ISimpleTitle simpleTitle)
			: base(simpleTitle)
		{
		}
		#endregion

		#region Public Properties
		public VariableDictionary MainSet { get; private set; }

		public IReadOnlyDictionary<string, VariableDictionary> VariableSets { get; private set; }
		#endregion

		#region Public Methods
		public string GetVariable(string name)
		{
			this.MainSet.TryGetValue(name, out var retval);
			return retval;
		}

		public string GetVariable(string setName, string name)
		{
			if (string.IsNullOrEmpty(setName))
			{
				return this.GetVariable(name);
			}
			else
			{
				if (this.VariableSets.TryGetValue(setName, out var set))
				{
					set.TryGetValue(name, out var retval);
					return retval;
				}

				return default;
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateCustomResults(PageItem pageItem)
		{
			ThrowNull(pageItem, nameof(pageItem));
			var varPageItem = pageItem as VariablesPageItem;
			var dictionary = new Dictionary<string, VariableDictionary>();
			foreach (var item in varPageItem.Variables)
			{
				if (item.Subset == null)
				{
					this.MainSet = item.Dictionary;
				}
				else
				{
					dictionary[item.Subset] = item.Dictionary;
				}
			}

			this.VariableSets = new ReadOnlyDictionary<string, VariableDictionary>(dictionary);
		}
		#endregion
	}
}