namespace RobinHood70.WikiCommon
{
	using System;

	/// <summary>Indicates to Code Analysis that a method validates a particular parameter.</summary>
	/// <remarks>Identical implementation to https://github.com/dotnet/corefx/blob/master/src/System.Collections.Immutable/src/Validation/ValidatedNotNullAttribute.cs </remarks>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class ValidatedNotNullAttribute : Attribute
	{
	}
}