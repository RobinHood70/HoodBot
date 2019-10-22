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
		#region Constructors
		internal UserInfoResult(long id, string name, DateTime? blockExpiry, long blockId, string? blockReason, DateTime? blockTimestamp, string? blockedBy, long blockedById, ChangeableGroupsInfo? changeableGroups, long editCount, string? email, DateTime? emailAuthenticated, UserInfoFlags flags, IReadOnlyList<string> groups, IReadOnlyList<string> implicitGroups, Dictionary<string, object> options, string? preferencesToken, Dictionary<string, RateLimitsItem?> rateLimits, string? realName, DateTime? registrationDate, IReadOnlyList<string> rights, string? unreadText)
		{
			this.Id = id;
			this.Name = name;
			this.BlockExpiry = blockExpiry;
			this.BlockId = blockId;
			this.BlockReason = blockReason;
			this.BlockTimestamp = blockTimestamp;
			this.BlockedBy = blockedBy;
			this.BlockedById = blockedById;
			this.ChangeableGroups = changeableGroups;
			this.EditCount = editCount;
			this.Email = email;
			this.EmailAuthenticated = emailAuthenticated;
			this.Flags = flags;
			this.Groups = groups;
			this.ImplicitGroups = implicitGroups;
			this.Options = options;
			this.PreferencesToken = preferencesToken;
			this.RateLimits = rateLimits;
			this.RealName = realName;
			this.RegistrationDate = registrationDate;
			this.Rights = rights;
			this.UnreadCount = -1;
			this.UnreadText = unreadText;
			if (unreadText != null)
			{
				if (unreadText.EndsWith("+", StringComparison.Ordinal))
				{
					unreadText = unreadText.Substring(0, unreadText.Length - 1);
				}

				if (int.TryParse(unreadText, out var result))
				{
					this.UnreadCount = result;
				}
			}
		}
		#endregion

		#region Public Properties
		public string? BlockedBy { get; }

		public long BlockedById { get; }

		public DateTime? BlockExpiry { get; }

		public long BlockId { get; }

		public string? BlockReason { get; }

		public DateTime? BlockTimestamp { get; }

		public ChangeableGroupsInfo? ChangeableGroups { get; }

		public long EditCount { get; }

		public string? Email { get; }

		public DateTime? EmailAuthenticated { get; }

		public UserInfoFlags Flags { get; }

		public IReadOnlyList<string> Groups { get; }

		public long Id { get; }

		public IReadOnlyList<string> ImplicitGroups { get; }

		public string Name { get; }

		public IReadOnlyDictionary<string, object> Options { get; }

		public string? PreferencesToken { get; }

		public IReadOnlyDictionary<string, RateLimitsItem> RateLimits { get; }

		public string? RealName { get; }

		public DateTime? RegistrationDate { get; }

		public IReadOnlyList<string> Rights { get; }

		public int UnreadCount { get; }

		public string? UnreadText { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}
