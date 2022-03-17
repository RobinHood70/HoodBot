namespace RobinHood70.HoodBot.Uesp
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class VariablesPage : Page
	{
		#region Fields
		private readonly Dictionary<string, IDictionary<string, string>> subsets = new(System.StringComparer.Ordinal);
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="VariablesPage"/> class.</summary>
		/// <param name="title">The <see cref="Title"/> to copy values from.</param>
		/// <param name="options">The load options used for this page. Can be used to detect if default-valued information is legitimate or was never loaded.</param>
		/// <param name="apiItem">The API item to extract information from.</param>
		public VariablesPage(Title title, PageLoadOptions options, IApiTitle? apiItem)
			: base(title, options, apiItem)
		{
			if (apiItem is VariablesPageItem varItem)
			{
				foreach (var item in varItem.Variables)
				{
					var subsetName = item.Subset ?? string.Empty;
					if (!this.subsets.TryGetValue(subsetName, out var subset))
					{
						subset = new Dictionary<string, string>(System.StringComparer.Ordinal);
						this.subsets.Add(subsetName, subset);
					}

					foreach (var entry in item.Dictionary)
					{
						subset[entry.Key] = entry.Value;
					}
				}
			}
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, string> MainSet => this.subsets.TryGetValue(string.Empty, out var mainSet)
			? mainSet.AsReadOnly()
			: ImmutableDictionary<string, string>.Empty;
		#endregion

		#region Public Methods
		public string? GetVariable(string name) =>
			this.MainSet != null && this.MainSet.TryGetValue(name, out var retval)
				? retval
				: default;

		public string? GetVariable(string setName, string name) =>
			this.subsets.TryGetValue(setName ?? string.Empty, out var set) &&
			set.TryGetValue(name, out var retval)
				? retval
				: null;
		#endregion
	}
}