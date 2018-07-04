namespace RobinHood70.HoodBot.Jobs
{
	using System;

	public enum ParameterType
	{
		Text,
		Boolean,
		Numeric
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
	public sealed class JobParameterAttribute : Attribute
	{
		public string Label { get; }

		public ParameterType Type { get; }
	}
}
