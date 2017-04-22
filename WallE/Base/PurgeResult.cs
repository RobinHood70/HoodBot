#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	[Flags]
	public enum PurgeFlags
	{
		None = 0,
		Invalid = 1,
		LinkUpdate = 1 << 1,
		Missing = 1 << 2,
		Purged = 1 << 3
	}

	public class PurgeResult : ITitle
	{
		#region Public Properties
		public PurgeFlags Flags { get; set; }

		public int? Namespace { get; set; }

		public long PageId { get; set; }

		public string Title { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}