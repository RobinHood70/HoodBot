#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class ImportItem : ITitleOnly
	{
		#region Public Properties
		public bool Invalid { get; set; }

		public int? Namespace { get; set; }

		public int Revisions { get; set; }

		public string Title { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
