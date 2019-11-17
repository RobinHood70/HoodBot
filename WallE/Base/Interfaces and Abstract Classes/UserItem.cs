#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class UserItem
	{
		#region Constructors
		internal UserItem(long userId, string name, string? blockedBy, long blockedById, DateTime? blockExpiry, bool blockHidden, long blockId, string? blockReason, DateTime? blockTimestamp, long editCount, IReadOnlyList<string>? groups, IReadOnlyList<string>? implicitGroups, DateTime? registration, IReadOnlyList<string>? rights)
		{
			this.UserId = userId;
			this.Name = name;
			this.BlockedBy = blockedBy;
			this.BlockedById = blockedById;
			this.BlockExpiry = blockExpiry;
			this.BlockHidden = blockHidden;
			this.BlockId = blockId;
			this.BlockReason = blockReason;
			this.BlockTimestamp = blockTimestamp;
			this.EditCount = editCount;
			this.Groups = groups;
			this.ImplicitGroups = implicitGroups;
			this.Registration = registration;
			this.Rights = rights;
		}
		#endregion

		#region Public Properties
		public string? BlockedBy { get; }

		public long BlockedById { get; }

		public DateTime? BlockExpiry { get; }

		public bool BlockHidden { get; }

		public long BlockId { get; }

		public string? BlockReason { get; }

		public DateTime? BlockTimestamp { get; }

		public long EditCount { get; }

		public IReadOnlyList<string>? Groups { get; }

		public IReadOnlyList<string>? ImplicitGroups { get; }

		public string Name { get; }

		public DateTime? Registration { get; }

		public IReadOnlyList<string>? Rights { get; }

		public long UserId { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}
