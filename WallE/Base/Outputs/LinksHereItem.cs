#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	public class LinksHereItem(int? ns, string? title, long pageId, bool redirect) : IApiTitleOptional
	{
		#region Public Properties
		public int? Namespace { get; } = ns;

		public long PageId { get; } = pageId;

		public bool Redirect { get; } = redirect;

		public string? Title { get; } = title;
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? FallbackText.NoTitle;
		#endregion
	}
}