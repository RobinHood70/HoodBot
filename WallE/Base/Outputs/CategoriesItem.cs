#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using RobinHood70.WikiCommon;

	public class CategoriesItem : IApiTitle
	{
		#region Constructors
		public CategoriesItem(int ns, string title, bool hidden, string? sortkey, string? sortkeyPrefix, DateTime? timestamp)
		{
			this.Namespace = ns;
			this.Title = title;
			this.Hidden = hidden;
			this.SortKey = sortkey;
			this.SortKeyPrefix = sortkeyPrefix;
			this.Timestamp = timestamp;
		}
		#endregion

		#region Public Properties
		public bool Hidden { get; }

		public int Namespace { get; }

		public string? SortKey { get; }

		public string? SortKeyPrefix { get; }

		public DateTime? Timestamp { get; }

		public string Title { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Title;
		#endregion
	}
}
