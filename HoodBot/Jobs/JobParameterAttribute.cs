namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics.CodeAnalysis;

	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	[SuppressMessage("Performance", "CA1813:Avoid unsealed attributes", Justification = "Inherited.")]
	public class JobParameterAttribute : Attribute
	{
		public object? DefaultValue { get; set; }

		public string? Label { get; set; }
	}
}