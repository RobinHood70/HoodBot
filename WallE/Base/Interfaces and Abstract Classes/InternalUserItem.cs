#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class InternalUserItem
	{
		#region Constructors
		internal InternalUserItem(long userId, string name)
		{
			this.Name = name;
			this.UserId = userId;
		}
		#endregion

		#region Public Properties
		public string? BlockedBy { get; internal set; }

		public long BlockedById { get; internal set; }

		public DateTime? BlockExpiry { get; internal set; }

		public bool BlockHidden { get; internal set; }

		public long BlockId { get; internal set; }

		public string? BlockReason { get; internal set; }

		public DateTime? BlockTimestamp { get; internal set; }

		public long EditCount { get; internal set; }

		public IReadOnlyList<string>? Groups { get; internal set; }

		public IReadOnlyList<string>? ImplicitGroups { get; internal set; }

		public string Name { get; }

		public DateTime? Registration { get; internal set; }

		public IReadOnlyList<string>? Rights { get; internal set; }

		public long UserId { get; }
		#endregion

	}
}
