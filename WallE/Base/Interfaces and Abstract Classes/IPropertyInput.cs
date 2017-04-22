namespace RobinHood70.WallE.Base
{
	using System.Diagnostics.CodeAnalysis;

	/// <summary>Used to limit the modules that can be passed to PagesLoad at compile time.</summary>
	[SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces", Justification = "Identifies a compile-time set of types.")]
	public interface IPropertyInput
	{
	}
}
