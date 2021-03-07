namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using static RobinHood70.CommonCode.Globals;

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
		public IReadOnlyDictionary<string, string>? MainSet => this.mainSet;

		public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> VariableSets => this.subsets;
		#endregion

		#region Public Methods
		public string? GetVariable(string name) =>
			this.MainSet != null && this.MainSet.TryGetValue(name, out var retval)
				? retval
				: default;

		public string? GetVariable(string setName, string name) =>
			string.IsNullOrEmpty(setName)
				? this.GetVariable(name) :
			this.VariableSets.TryGetValue(setName, out var set) && set.TryGetValue(name, out var retval)
				? retval
				: default;
		#endregion

		#region Protected Override Methods
		protected override void PopulateCustomResults(PageItem pageItem)
		{
			ThrowNull(pageItem, nameof(pageItem));
			if (pageItem is VariablesPageItem varPageItem)
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