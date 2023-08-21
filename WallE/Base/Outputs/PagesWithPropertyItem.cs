#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	// This class is not an ITitle because none of the properties are guaranteed to be emitted as of MW 1.33.
	public class PagesWithPropertyItem : IApiTitleOptional
	{
		#region Constructors
		public PagesWithPropertyItem(int? ns, string? title, long pageId, string? value)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
			this.Value = value;
		}
		#endregion

		#region Public Properties
		public int? Namespace { get; }

		public long PageId { get; }

		public string? Title { get; }

		public string? Value { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? this.Value ?? FallbackText.NoTitle;
		#endregion
	}
}
