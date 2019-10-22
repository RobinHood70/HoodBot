#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class UndeleteResult : ITitleOptional
	{
		#region Constructors
		internal UndeleteResult(string title, int revisions, int fileVersions, string reason)
		{
			this.Title = title;
			this.Revisions = revisions;
			this.FileVersions = fileVersions;
			this.Reason = reason;
		}
		#endregion

		#region Public Properties
		public int FileVersions { get; }

		public string Reason { get; }

		public int Revisions { get; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
