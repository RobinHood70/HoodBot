#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public interface IUser
	{
		#region Properties
		string BlockedBy { get; set; }

		long BlockedById { get; set; }

		DateTime? BlockExpiry { get; set; }

		bool BlockHidden { get; set; }

		long BlockId { get; set; }

		string BlockReason { get; set; }

		DateTime? BlockTimestamp { get; set; }

		long EditCount { get; set; }

		IReadOnlyList<string> Groups { get; set; }

		IReadOnlyList<string> ImplicitGroups { get; set; }

		string Name { get; set; }

		DateTime? Registration { get; set; }

		IReadOnlyList<string> Rights { get; set; }

		long UserId { get; set; }
		#endregion
	}
}
