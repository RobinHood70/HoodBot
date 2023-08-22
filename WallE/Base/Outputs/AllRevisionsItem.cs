#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon;

	public class AllRevisionsItem : IApiTitle
	{
		#region Constructors
		internal AllRevisionsItem(int ns, string title, long pageId, IReadOnlyList<RevisionItem> revisions)
		{
			this.Namespace = ns;
			this.Title = title.NotNull();
			this.PageId = pageId;
			this.Revisions = revisions;
		}
		#endregion

		#region Public Properties
		public int Namespace { get; }

		public long PageId { get; }

		public IReadOnlyList<RevisionItem> Revisions { get; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}