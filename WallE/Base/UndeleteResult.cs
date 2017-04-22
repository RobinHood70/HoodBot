#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class UndeleteResult
	{
		#region Public Properties
		public int FileVersions { get; set; }

		public string Reason { get; set; }

		public int Revisions { get; set; }

		public string Title { get; set; }
		#endregion
	}
}
