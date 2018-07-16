#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	#region Public Enumerations
	[Flags]
	public enum UserInfoFlags
	{
		None = 0,
		Anonymous = 1,
		HasMessage = 1 << 1
	}
	#endregion

	public class UserInfoResult
	{
		#region Public Properties
		public string BlockedBy { get; set; }

		public long BlockedById { get; set; }

		public DateTime? BlockExpiry { get; set; }

		public long BlockId { get; set; }

		public string BlockReason { get; set; }

		public DateTime? BlockTimestamp { get; set; }

		public ChangeableGroupsInfo ChangeableGroups { get; set; }

		public long EditCount { get; set; }

		public string Email { get; set; }

		public DateTime? EmailAuthenticated { get; set; }

		public UserInfoFlags Flags { get; set; }

		public IReadOnlyList<string> Groups { get; set; }

		public long Id { get; set; }

		public IReadOnlyList<string> ImplicitGroups { get; set; }

		public string Name { get; set; }

		public IReadOnlyDictionary<string, object> Options { get; set; }

		public string PreferencesToken { get; set; }

		public IReadOnlyDictionary<string, RateLimitsItem> RateLimits { get; set; }

		public string RealName { get; set; }

		public DateTime? RegistrationDate { get; set; }

		public IReadOnlyList<string> Rights { get; set; }

		public int UnreadCount { get; set; }
		#endregion
	}
}
