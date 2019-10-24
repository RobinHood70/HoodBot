#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;
	using RobinHood70.WikiCommon;

	public class LogEventsItem : LogEvent, ITitleOptional
	{
		#region Constructors
		internal LogEventsItem(int? ns, string? title, long logPageId, IReadOnlyList<string> tags)
		{
			this.Namespace = ns;
			this.Title = title;
			this.LogPageId = logPageId;
			this.Tags = tags;
		}
		#endregion

		#region Public Properties
		public long LogPageId { get; }

		public int? Namespace { get; }

		public IReadOnlyList<string> Tags { get; }

		public string? Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title ?? Globals.NoTitle;
		#endregion
	}
}
