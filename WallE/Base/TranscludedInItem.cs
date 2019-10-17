#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class TranscludedInItem : ITitleOptional
	{
		#region Constructors
		internal TranscludedInItem(int? ns, string? title, long pageId, bool redirect)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
			this.Redirect = redirect;
		}
		#endregion

		#region Public Properties
		public int? Namespace { get; }

		public bool Redirect { get; }

		public long PageId { get; }

		public string? Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? ProjectGlobals.NoTitle;
		#endregion
	}
}
