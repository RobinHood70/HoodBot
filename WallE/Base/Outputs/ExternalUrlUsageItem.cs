#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	// Despite looking like an ITitle, we cannot guarantee that it will behave like once since, at least as of MW 1.33, no part of the result is required to be emitted by the API.
	public class ExternalUrlUsageItem : ITitleOptional
	{
		#region Constructors
		internal ExternalUrlUsageItem(int? ns, string? title, long pageId, string? url)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
			this.Url = url;
		}
		#endregion

		#region Public Properties
		public int? Namespace { get; }

		public long PageId { get; }

		public string? Title { get; }

		public string? Url { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? this.Url ?? FallbackText.NoTitle;
		#endregion
	}
}
