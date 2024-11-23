namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;

// CONSIDER: For now, class is strictly OrdinalIgnoreCase, but could be expanded if needed.
public sealed class PairedStringComparer : IComparer<KeyValuePair<string, string>>
{
	#region Fields
	private static readonly Lazy<PairedStringComparer> LazyInstance = new(() => new PairedStringComparer());
	#endregion

	#region Constructors
	private PairedStringComparer()
	{
	}
	#endregion

	#region Public Properties
	public static PairedStringComparer Instance => LazyInstance.Value;
	#endregion

	#region Public Methods
	public int Compare(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
	{
		var compare = string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
		return compare == 0
			? string.Compare(x.Value, y.Value, StringComparison.OrdinalIgnoreCase)
			: compare;
	}
	#endregion
}