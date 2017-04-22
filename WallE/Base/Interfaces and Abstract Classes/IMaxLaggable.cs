#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	/// <summary>Add this interface to a WikiAbstractionLayer to mark it as allowing maxlag.</summary>
	/// <remarks>There is no functionality associated with this interface; that's left to the abstraction layer itself. This is here solely for convenience, since both index.php and api.php can support maxlag.</remarks>
	public interface IMaxLaggable
	{
		#region Properties
		int MaxLag { get; set; }
		#endregion
	}
}
