#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class LanguageBacklinksItem : ITitle
	{
		#region Constructors
		internal LanguageBacklinksItem(int ns, string title, long pageId, bool isRedirect, string? langCode, string? langTitle)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
			this.IsRedirect = isRedirect;
			this.LanguageCode = langCode;
			this.LanguageTitle = langTitle;
		}
		#endregion

		#region Public Properties
		public bool IsRedirect { get; set; }

		public string? LanguageCode { get; set; }

		public string? LanguageTitle { get; set; }

		public int Namespace { get; }

		public long PageId { get; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
