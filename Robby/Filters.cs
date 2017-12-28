namespace RobinHood70.Robby
{
	using System;
	using RobinHood70.WikiCommon;

	#region Public Enumerations
	[Flags]
	public enum RecentChangesFilters
	{
		None = 0,
		Anonymous = 1,
		Bot = 1 << 1,
		Minor = 1 << 2,
		Patrolled = 1 << 3,
		Redirect = 1 << 4,
		All,
	}
	#endregion

	internal static class Filters
	{
		#region Internal Static Methods
		public static Filter FlagToFilter(Enum showOnly, Enum hide, Enum flag) =>
			hide.HasFlag(flag) ? Filter.Exclude :
			showOnly.HasFlag(flag) ? Filter.Only :
			Filter.Any;
		#endregion
	}
}