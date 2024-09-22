#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	[Flags]
	public enum PurgeFlags
	{
		None = 0,
		Invalid = 1,
		LinkUpdate = 1 << 1,
		Missing = 1 << 2,
		Purged = 1 << 3
	}

	public class PurgeItem : IApiTitle
	{
		#region Constructors
		internal PurgeItem(int ns, string title, long pageId, PurgeFlags flags)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
			this.Flags = flags;
		}
		#endregion

		#region Public Properties
		public PurgeFlags Flags { get; internal set; }

		public int Namespace { get; }

		public long PageId { get; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}