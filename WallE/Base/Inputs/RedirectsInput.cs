#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	#region Public Enumerations
	[Flags]
	public enum RedirectsProperties
	{
		None = 0,
		PageId = 1,
		Title = 1 << 1,
		Fragment = 1 << 2
	}
	#endregion

	public class RedirectsInput : IPropertyInput, ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public Filter FilterFragments { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int>? Namespaces { get; set; }

		public RedirectsProperties Properties { get; set; }
		#endregion
	}
}
