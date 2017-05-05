namespace RobinHood70.Robby
{
	using System;

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
}
