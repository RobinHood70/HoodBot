#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using RobinHood70.WikiCommon;

	public class UndeleteResult : IApiTitle
	{
		#region Constructors
		internal UndeleteResult(int ns, string title, int revisions, int fileVersions, string reason)
		{
			this.Namespace = ns;
			this.FullPageName = title;
			this.Revisions = revisions;
			this.FileVersions = fileVersions;
			this.Reason = reason;
		}
		#endregion

		#region Public Properties
		public int FileVersions { get; }

		public int Namespace { get; }

		public string Reason { get; }

		public int Revisions { get; }

		public string FullPageName { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.FullPageName;
		#endregion
	}
}
