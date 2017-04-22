#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public interface ILinksInput : IPropertyInput, ILimitableInput, IGeneratorInput
	{
		#region Properties
		IEnumerable<int> Namespaces { get; set; }

		bool SortDescending { get; set; }
		#endregion
	}
}
