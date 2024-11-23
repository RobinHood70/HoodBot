#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

public class AllUsersItem : UserItem
{
	#region Constructors
	internal AllUsersItem(UserItem baseUser, int recentActions)
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
		this.RecentActions = recentActions;
	}
	#endregion

	#region Public Properties
	public int RecentActions { get; }
	#endregion
}