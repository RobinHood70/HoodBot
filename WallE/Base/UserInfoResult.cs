#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using System.Collections.Generic;
using System.Globalization;

#region Public Enumerations
[Flags]
public enum UserInfoFlags
{
	None = 0,
	Anonymous = 1,
	HasMessage = 1 << 1
}
#endregion

// Note that BlockHidden will always be false for this class, since block information is never hidden from the user themselves.
public class UserInfoResult : UserItem
{
	#region Constructors
	internal UserInfoResult(UserItem baseUser, ChangeableGroupsInfo? changeableGroups, string? email, DateTime? emailAuthenticated, UserInfoFlags flags, IReadOnlyDictionary<string, object> options, string? preferencesToken, IReadOnlyDictionary<string, RateLimitsItem?> rateLimits, string? realName, string? unreadText)
		: base(
			  userId: baseUser.UserId,
			  name: baseUser.Name,
			  blockedBy: baseUser.BlockedBy,
			  blockedById: baseUser.BlockedById,
			  blockExpiry: baseUser.BlockExpiry,
			  blockHidden: baseUser.BlockHidden,
			  blockId: baseUser.BlockId,
			  blockReason: baseUser.BlockReason,
			  blockTimestamp: baseUser.BlockTimestamp,
			  editCount: baseUser.EditCount,
			  groups: baseUser.Groups,
			  implicitGroups: baseUser.ImplicitGroups,
			  registration: baseUser.Registration,
			  rights: baseUser.Rights)
	{
		this.ChangeableGroups = changeableGroups;
		this.Email = email;
		this.EmailAuthenticated = emailAuthenticated;
		this.Flags = flags;
		this.Options = options;
		this.PreferencesToken = preferencesToken;
		this.RateLimits = rateLimits;
		this.RealName = realName;
		this.UnreadCount = -1;
		this.UnreadText = unreadText;
		if (unreadText?.Length > 0)
		{
			unreadText = unreadText.TrimEnd('+');
			if (int.TryParse(unreadText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				this.UnreadCount = result;
			}
		}
	}
	#endregion

	#region Public Properties
	public ChangeableGroupsInfo? ChangeableGroups { get; }

	public string? Email { get; }

	public DateTime? EmailAuthenticated { get; }

	public UserInfoFlags Flags { get; }

	public IReadOnlyDictionary<string, object> Options { get; }

	public string? PreferencesToken { get; }

	public IReadOnlyDictionary<string, RateLimitsItem?> RateLimits { get; }

	public string? RealName { get; }

	public int UnreadCount { get; }

	public string? UnreadText { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Name;
	#endregion
}