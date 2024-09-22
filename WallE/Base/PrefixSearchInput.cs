#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class PrefixSearchInput(string search) : ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int>? Namespaces { get; set; }

		public string Search { get; } = search;
		#endregion
	}
}