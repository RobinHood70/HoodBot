#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using WikiCommon;

	public class BacklinksItem : ITitle
	{
		#region Public Properties
		public bool IsRedirect { get; set; }

		public int? Namespace { get; set; }

		public long PageId { get; set; }

		public IReadOnlyList<ITitle> Redirects { get; } = new List<ITitle>();

		public string Title { get; set; }

		public BacklinksTypes Type { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
