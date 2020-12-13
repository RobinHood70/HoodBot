namespace RobinHood70.HoodBot.Jobs
{
	using System;

	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public class JobParameterAttribute : Attribute
	{
		public object? DefaultValue { get; set; }

		public string? Label { get; set; }
	}
}