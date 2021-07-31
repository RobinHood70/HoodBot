namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class VariablesPage : Page
	{
		#region Fields
		private readonly Dictionary<string, string> mainSet = new(System.StringComparer.Ordinal);
		private readonly Dictionary<string, IReadOnlyDictionary<string, string>> subsets = new(System.StringComparer.Ordinal);
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="VariablesPage"/> class.</summary>
		/// <param name="title">The <see cref="ISimpleTitle"/> to copy values from.</param>
		/// <param name="options">The load options used for this page. Can be used to detect if default-valued information is legitimate or was never loaded.</param>
		/// <param name="apiItem">The API item to extract information from.</param>
		public VariablesPage(ISimpleTitle title, PageLoadOptions options, IApiTitle? apiItem)
			: base(title, options, apiItem)
		{
			if (apiItem is VariablesPageItem varItem)
			{
				foreach (var item in varItem.Variables)
				{
					if (item.Subset == null)
					{
						this.mainSet.AddRange(item.Dictionary);
					}
					else
					{
						this.subsets.Add(item.Subset, item.Dictionary);
					}
				}
			}
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
	}
}