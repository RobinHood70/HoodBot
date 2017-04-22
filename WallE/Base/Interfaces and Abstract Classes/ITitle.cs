#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	/// <summary>Generic class for title return values.</summary>
	/// <remarks>While there are some variants in the MediaWiki code, nearly all title-like return values return NS and Title, while most also return PageId as well. When PageId is not returned, it will be very obvious, since a PageId of 0 is generally impossible, though deleted revisisions sometimes use a PageId of 0 as well. This has therefore been made into an object to ensure uniformity throughout the project, despite the fact that this will sometimes mean returning non-existent values.</remarks>
	public interface ITitle : ITitleOnly
	{
		#region Public Properties
		long PageId { get; set; }
		#endregion
	}
}