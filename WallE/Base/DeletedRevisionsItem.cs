#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class DeletedRevisionsItem : AllRevisionsItem
	{
		#region Constructors
		public DeletedRevisionsItem(int ns, string title, long pageId, IReadOnlyList<RevisionsItem> revisions, string? token)
			: base(ns, title, pageId, revisions) => this.DeletedRevisionsToken = token;
		#endregion

		#region Public Properties
		public string? DeletedRevisionsToken { get; set; }
		#endregion
	}
}
