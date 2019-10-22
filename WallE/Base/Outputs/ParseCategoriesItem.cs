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
		#region Constructors
		internal ParseCategoriesItem(string category, string sortKey, ParseCategoryFlags flags)
		{
			this.Category = category;
			this.SortKey = sortKey;
			this.Flags = flags;
		}
		#endregion

		#region Public Properties
		public string Category { get; }

		public ParseCategoryFlags Flags { get; }

		public string SortKey { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Category;
		#endregion
	}
}
