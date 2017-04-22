#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	public enum Gender
	{
		Unknown,
		Male,
		Female
	}

	[Flags]
	public enum UserFlags
	{
		None = 0,
		Emailable = 1,
		Interwiki = 1 << 1,
		Invalid = 1 << 2,
		Missing = 1 << 3
	}
	#endregion

	public class UsersItem : IUser
	{
		#region Public Properties
		public string BlockedBy { get; set; }

		public long BlockedById { get; set; }

		public DateTime? BlockExpiry { get; set; }

		public bool BlockHidden { get; set; }

		public long BlockId { get; set; }

		public string BlockReason { get; set; }

		public DateTime? BlockTimestamp { get; set; }

		public long EditCount { get; set; }

		public UserFlags Flags { get; set; }

		public Gender Gender { get; set; }

		public IReadOnlyList<string> Groups { get; set; }

		public IReadOnlyList<string> ImplicitGroups { get; set; }

		public string Name { get; set; }

		public DateTime? Registration { get; set; }

		public IReadOnlyList<string> Rights { get; set; }

		public string Token { get; set; }

		public long UserId { get; set; }
		#endregion
	}
}
