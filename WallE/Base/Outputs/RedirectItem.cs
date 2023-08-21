#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	public class RedirectItem : IApiTitleOptional
	{
		#region Constructors
		internal RedirectItem(int? ns, string? title, long pageId, string? fragment)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
			this.Fragment = fragment;
		}
		#endregion

		#region Public Properties
		public string? Fragment { get; }

		public int? Namespace { get; }

		public long PageId { get; }

		public string? Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? FallbackText.NoTitle;
		#endregion
	}
}
