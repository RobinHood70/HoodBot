#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class LanguageBacklinksItem : ITitle
	{
		#region Public Properties
		public bool IsRedirect { get; set; }

		public string LanguageCode { get; set; }

		public string LanguageTitle { get; set; }

		public int? Namespace { get; set; }

		public long PageId { get; set; }

		public string Title { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
