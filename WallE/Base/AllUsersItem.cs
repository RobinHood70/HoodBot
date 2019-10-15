#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using static RobinHood70.WikiCommon.Globals;

	public class AllUsersItem : IUser
	{
		#region Constructors
		public AllUsersItem(long userId, string? name)
		{
			this.Name = name ?? throw ArgumentNull(nameof(name));
			this.UserId = userId;
		}
		#endregion

		#region Public Properties
		public string? BlockedBy { get; set; }

		public long BlockedById { get; set; }

		public DateTime? BlockExpiry { get; set; }

		public bool BlockHidden { get; set; }

		public long BlockId { get; set; }

		public string? BlockReason { get; set; }

		public DateTime? BlockTimestamp { get; set; }

		public long EditCount { get; set; }

		public IReadOnlyList<string>? Groups { get; set; }

		public IReadOnlyList<string>? ImplicitGroups { get; set; }

		public string Name { get; set; }

		public int RecentActions { get; set; }

		public DateTime? Registration { get; set; }

		public IReadOnlyList<string>? Rights { get; set; }

		public long UserId { get; set; }
		#endregion
	}
}
