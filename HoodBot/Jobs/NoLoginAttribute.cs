namespace RobinHood70.HoodBot.Jobs
{
	using System;

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor)]
	public sealed class NoLoginAttribute : Attribute
	{
		#region Constructors
		public NoLoginAttribute()
		{
		}
		#endregion
	}
}