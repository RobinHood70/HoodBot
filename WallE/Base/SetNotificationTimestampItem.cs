#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
	[Flags]
	public enum SetNotificationTimestampFlags
	{
		None = 0,
		Invalid = 1,
		Known = 1 << 1,
		Missing = 1 << 2,
		NotWatched = 1 << 3
	}
	#endregion

	public class SetNotificationTimestampItem : ITitle
	{
		#region Constructors
		public SetNotificationTimestampItem(int ns, string title, long pageId)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
		}
		#endregion

		#region Public Properties
		public SetNotificationTimestampFlags Flags { get; set; }

		public int Namespace { get; }

		public DateTime? NotificationTimestamp { get; set; }

		public long PageId { get; }

		public long RevisionId { get; set; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}