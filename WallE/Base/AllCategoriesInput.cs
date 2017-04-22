#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum AllCategoriesProperties
	{
		None = 0,
		Size = 1,
		Hidden = 1 << 1,
		All = Size | Hidden
	}
	#endregion

	public class AllCategoriesInput : ILimitableInput, IGeneratorInput
	{
		#region Public Properties
		public string From { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public int MaxCount { get; set; } = -1;

		public int MinCount { get; set; } = -1;

		public string Prefix { get; set; }

		public AllCategoriesProperties Properties { get; set; }

		public bool SortDescending { get; set; }

		public string To { get; set; }
		#endregion
	}
}
