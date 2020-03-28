#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	#region Public Enumerations
	[Flags]
	public enum CategoriesProperties
	{
		None = 0,
		SortKey = 1,
		Timestamp = 1 << 1,
		Hidden = 1 << 2,
		All = SortKey | Timestamp | Hidden
	}
	#endregion

	public class CategoriesInput : IPropertyInput, ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public IEnumerable<string>? Categories { get; set; }

		public Filter FilterHidden { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public CategoriesProperties Properties { get; set; }

		public bool SortDescending { get; set; }
		#endregion
	}
}
