namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "Intentionally inheritable.")]
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public class JobParameterAttribute : Attribute
	{
		public object DefaultValue { get; set; }

		public string Label { get; set; }
	}
}