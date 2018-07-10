namespace RobinHood70.HoodBot.Jobs.Design
{
	using System;

	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
	public sealed class JobParameterAttribute : Attribute
	{
		public JobParameterAttribute(string label) => this.Label = label;

		public JobParameterAttribute(string label, object defaultValue)
			: this(label) => this.DefaultValue = defaultValue;

		public object DefaultValue { get; }

		public string Label { get; }

		// Bad coding practice, but saves creating a separate type for almost the same info.
		internal object CurrentValue { get; set; }
	}
}