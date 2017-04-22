#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum WatchlistRawProperties
	{
		None = 0,
		Changed = 1,
		All = Changed
	}
	#endregion

	public class WatchlistRawInput : ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int> Namespaces { get; set; }

		public string Owner { get; set; }

		// TODO: Check the difference between Properties and ShowChanged when Internet is back up.
		public WatchlistRawProperties Properties { get; set; }

		public FilterOption FilterChanged { get; set; }

		public bool SortDescending { get; set; }

		public string Token { get; set; }
		#endregion
	}
}
