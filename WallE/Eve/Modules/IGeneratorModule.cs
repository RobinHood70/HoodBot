#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces", Justification = "Identifies a compile-time set of types.")]
	public interface IGeneratorModule : IQueryModule
	{
		/// <summary>Flags this instance as being the current generator.</summary>
		void SetAsGenerator();
	}
}
