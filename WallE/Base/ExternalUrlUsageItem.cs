#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class ExternalUrlUsageItem : ITitle
	{
		#region Public Properties
		public int? Namespace { get; set; }

		public long PageId { get; set; }

		public string Title { get; set; }

		public string Url { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
