#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	public class CategoriesItem(int ns, string title, bool hidden, string? sortkey, string? sortkeyPrefix, DateTime? timestamp) : IApiTitle
	{
		#region Public Properties
		public bool Hidden { get; } = hidden;

		public int Namespace { get; } = ns;

		public string? SortKey { get; } = sortkey;

		public string? SortKeyPrefix { get; } = sortkeyPrefix;

		public DateTime? Timestamp { get; } = timestamp;

		public string Title { get; } = title;
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}