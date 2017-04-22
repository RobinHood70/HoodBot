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
		#region Public Properties
		public WatchFlags Flags { get; set; }

		public int? Namespace { get; set; }

		public long PageId { get; set; }

		public string Title { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
