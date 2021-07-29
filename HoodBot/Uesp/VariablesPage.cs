namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;

	public class VariablesPage : Page
	{
		#region Fields
		private readonly Dictionary<string, string> mainSet = new(System.StringComparer.Ordinal);
		private readonly Dictionary<string, IReadOnlyDictionary<string, string>> subsets = new(System.StringComparer.Ordinal);
		#endregion

		#region Constructors
		public VariablesPage(ISimpleTitle title)
			: base(title)
		{
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, string> MainSet => this.mainSet;

		public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> VariableSets => this.subsets;
		#endregion

		#region Public Methods
		public string? GetVariable(string name) =>
			this.MainSet != null && this.MainSet.TryGetValue(name, out var retval)
				? retval
				: default;

		public string? GetVariable(string setName, string name)
		{
			IReadOnlyDictionary<string, string> set;
			if (string.IsNullOrEmpty(setName))
			{
				set = this.MainSet;
			}
			else if (this.VariableSets.TryGetValue(setName, out var set2))
			{
				set = set2;
			}
			else
			{
				return null;
			}

			set.TryGetValue(name, out var retval);
			return retval;
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateCustomResults(PageItem pageItem)
		{
			if (pageItem.NotNull(nameof(pageItem)) is VariablesPageItem varPageItem)
			{
				this.mainSet.Clear();
				this.subsets.Clear();
				foreach (var item in varPageItem.Variables)
				{
					if (item.Subset == null)
					{
						this.mainSet.Clear();
						this.mainSet.AddRange(item.Dictionary);
					}
					else
					{
						this.subsets[item.Subset] = item.Dictionary;
					}
				}
			}
		}
		#endregion
	}
}