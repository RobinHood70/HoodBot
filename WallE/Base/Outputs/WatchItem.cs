#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum WatchFlags
	{
		None = 0,
		Watched = 1,
		Unwatched = 1 << 1,
		Missing = 1 << 2
	}
	#endregion

	public class WatchItem : ITitle
	{
		#region Constructors
		internal WatchItem(int ns, string title, long pageId, WatchFlags flags)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
			this.Flags = flags;
		}
		#endregion

		#region Public Properties
		public WatchFlags Flags { get; internal set; }

		public int Namespace { get; }

		public long PageId { get; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
