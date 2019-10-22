#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class ImportItem : ITitle
	{
		#region Constructors
		internal ImportItem(int ns, string title, int revisions, bool invalid)
		{
			this.Namespace = ns;
			this.Title = title;
			this.Revisions = revisions;
			this.Invalid = invalid;
		}
		#endregion

		#region Public Properties
		public bool Invalid { get; }

		public int Namespace { get; }

		public int Revisions { get; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
