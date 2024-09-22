#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum InfoProperties
	{
		None = 0,
		Protection = 1,
		TalkId = 1 << 1,
		Watched = 1 << 2,
		Watchers = 1 << 3,
		NotificationTimestamp = 1 << 4,
		SubjectId = 1 << 5,
		Url = 1 << 6,
		Readable = 1 << 7,
		Preload = 1 << 8,
		DisplayTitle = 1 << 9,
		All = Protection | TalkId | Watched | Watchers | NotificationTimestamp | SubjectId | Url | Readable | Preload | DisplayTitle
	}
	#endregion

	public class InfoInput : IPropertyInput
	{
		#region Public Properties
		public InfoProperties Properties { get; set; }

		public IEnumerable<string>? TestActions { get; set; }

		public IEnumerable<string>? Tokens { get; set; }
		#endregion
	}
}