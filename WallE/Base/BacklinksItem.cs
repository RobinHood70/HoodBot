#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	public class BacklinksItem : ITitle
	{
		#region Constructors
		internal BacklinksItem(int ns, string title, long pageId, bool isRedirect, BacklinksTypes type)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
			this.IsRedirect = isRedirect;
			this.Type = type;
		}
		#endregion

		#region Public Properties
		public bool IsRedirect { get; }

		public int Namespace { get; }

		public long PageId { get; }

		public IReadOnlyList<ITitle> Redirects { get; } = new List<ITitle>();

		public string Title { get; }

		public BacklinksTypes Type { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
