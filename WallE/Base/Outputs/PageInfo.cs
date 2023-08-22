#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum PageInfoFlags
	{
		None = 0,
		New = 1,
		Readable = 1 << 1,
		Redirect = 1 << 2,
		Watched = 1 << 3
	}
	#endregion

	public class PageInfo
	{
		#region Constructors
		internal PageInfo(Uri? canonicalUrl, string? contentModel, long counter, string? displayTitle, Uri? editUrl, PageInfoFlags flags, Uri? fullUrl, string? language, long lastRevisionId, int length, DateTime? notificationTimestamp, string? preload, List<ProtectionsItem> protections, IReadOnlyList<string> restrictionTypes, DateTime? startTimestamp, long subjectId, long talkId, Dictionary<string, bool> testActions, Dictionary<string, string> tokens, DateTime? touched, long watchers)
		{
			this.CanonicalUrl = canonicalUrl;
			this.ContentModel = contentModel;
			this.Counter = counter;
			this.DisplayTitle = displayTitle;
			this.EditUrl = editUrl;
			this.Flags = flags;
			this.FullUrl = fullUrl;
			this.Language = language;
			this.LastRevisionId = lastRevisionId;
			this.Length = length;
			this.NotificationTimestamp = notificationTimestamp;
			this.Preload = preload;
			this.Protections = protections;
			this.RestrictionTypes = restrictionTypes;
			this.StartTimestamp = startTimestamp;
			this.SubjectId = subjectId;
			this.TalkId = talkId;
			this.TestActions = testActions;
			this.Tokens = tokens;
			this.Touched = touched;
			this.Watchers = watchers;
		}
		#endregion

		#region Public Properties
		public Uri? CanonicalUrl { get; }

		public string? ContentModel { get; }

		public long Counter { get; }

		public string? DisplayTitle { get; }

		public Uri? EditUrl { get; }

		public PageInfoFlags Flags { get; }

		public Uri? FullUrl { get; }

		public string? Language { get; }

		public long LastRevisionId { get; }

		public int Length { get; }

		public DateTime? NotificationTimestamp { get; }

		public string? Preload { get; }

		public IReadOnlyList<ProtectionsItem> Protections { get; }

		public IReadOnlyList<string> RestrictionTypes { get; }

		public DateTime? StartTimestamp { get; }

		public long SubjectId { get; }

		public long TalkId { get; }

		public IReadOnlyDictionary<string, bool> TestActions { get; }

		public IReadOnlyDictionary<string, string> Tokens { get; }

		public DateTime? Touched { get; }

		public long Watchers { get; }
		#endregion
	}
}