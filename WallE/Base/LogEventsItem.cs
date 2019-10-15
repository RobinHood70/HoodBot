#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class LogEventsItem : ILogEvents, ITitle
	{
		#region Constructors
		public LogEventsItem(int ns, string title, long pageId)
		{
			this.Namespace = ns;
			this.Title = title;
			this.PageId = pageId;
		}
		#endregion

		#region Public Properties
		public string Comment { get; set; }

		public IReadOnlyDictionary<string, object> ExtraData { get; set; }

		public LogEventFlags LogEventFlags { get; set; }

		public string LogAction { get; set; }

		public long LogId { get; set; }

		public long LogPageId { get; set; }

		public string LogType { get; set; }

		public long PageId { get; }

		public string ParsedComment { get; set; }

		public int Namespace { get; }

		public IReadOnlyList<string> Tags { get; set; }

		public DateTime? Timestamp { get; set; }

		public string Title { get; }

		public string User { get; set; }

		public long UserId { get; set; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
