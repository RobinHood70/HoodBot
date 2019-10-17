#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;

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
		#region Public Properties
		public Uri CanonicalUrl { get; set; }

		public string ContentModel { get; set; }

		public long Counter { get; set; }

		public string DisplayTitle { get; set; }

		public Uri EditUrl { get; set; }

		public PageInfoFlags Flags { get; set; }

		public Uri FullUrl { get; set; }

		public string Language { get; set; }

		public long LastRevisionId { get; set; }

		public int Length { get; set; }

		public DateTime? NotificationTimestamp { get; set; }

		public string Preload { get; set; }

		public IReadOnlyList<ProtectionsItem> Protections { get; set; } = Array.Empty<ProtectionsItem>();

		public IReadOnlyList<string> RestrictionTypes { get; set; } = Array.Empty<string>();

		public DateTime? StartTimestamp { get; set; }

		public long SubjectId { get; set; }

		public long TalkId { get; set; }

		public IReadOnlyDictionary<string, bool> TestActions { get; set; } = ImmutableDictionary<string, bool>.Empty;

		public IReadOnlyDictionary<string, string> Tokens { get; set; } = ImmutableDictionary<string, string>.Empty;

		public DateTime? Touched { get; set; }

		public long Watchers { get; set; }
		#endregion
	}
}
