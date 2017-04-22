#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public interface ITitleOnly
	{
		#region Properties
		// Namespace information can be absent with invalid titles or missing pageids/revids. Since there is no value that's absolutely guaranteed not to be used by anyone, we use a nullable value instead.
		int? Namespace { get; set; }

		string Title { get; set; }
		#endregion
	}
}