#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using RobinHood70.CommonCode;
	using static RobinHood70.CommonCode.Globals;

	public class PageSetResult<T> : ReadOnlyKeyedCollection<string, T>, IPageSetResult
		where T : ITitle
	{
		#region Constructors
		internal PageSetResult(IEnumerable<T> titles)
			: this(
				  titles,
				  Array.Empty<long>(),
				  ImmutableDictionary<string, string>.Empty,
				  ImmutableDictionary<string, InterwikiTitleItem>.Empty,
				  ImmutableDictionary<string, string>.Empty,
				  ImmutableDictionary<string, PageSetRedirectItem>.Empty)
		{
		}

		internal PageSetResult(
			IEnumerable<T> titles,
			IReadOnlyList<long> badRevisionIds,
			IReadOnlyDictionary<string, string> converted,
			IReadOnlyDictionary<string, InterwikiTitleItem> interwiki,
			IReadOnlyDictionary<string, string> normalized,
			IReadOnlyDictionary<string, PageSetRedirectItem> redirects)
			: base((item) => item?.Title ?? throw ArgumentNull(nameof(item)), titles)
		{
			this.BadRevisionIds = badRevisionIds;
			this.Converted = converted;
			this.Interwiki = interwiki;
			this.Normalized = normalized;
			this.Redirects = redirects;
		}
		#endregion

		#region Properties
		public IReadOnlyList<long> BadRevisionIds { get; }

		public IReadOnlyDictionary<string, string> Converted { get; }

		public IReadOnlyDictionary<string, InterwikiTitleItem> Interwiki { get; }

		public IReadOnlyDictionary<string, string> Normalized { get; }

		public IReadOnlyDictionary<string, PageSetRedirectItem> Redirects { get; }
		#endregion
	}
}