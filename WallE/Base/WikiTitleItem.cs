#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class WikiTitleItem : ITitle
	{
		#region Constructors
		public WikiTitleItem(int ns, string title, long pageId)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
		}
		#endregion

		#region Public Properties
		public int Namespace { get; }

		public long PageId { get; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
