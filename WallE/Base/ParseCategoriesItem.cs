#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum ParseCategoryFlags
	{
		None = 0,
		Hidden = 1,
		Known = 1 << 1,
		Missing = 1 << 2
	}
	#endregion

	public class ParseCategoriesItem
	{
		#region Public Properties
		public string Category { get; set; }

		public ParseCategoryFlags Flags { get; set; }

		public string SortKey { get; set; }
		#endregion
	}
}
