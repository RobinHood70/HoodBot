#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	public class RandomInput : ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public Filter FilterRedirects { get; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int>? Namespaces { get; set; }
		#endregion
	}
}
