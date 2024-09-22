#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	#region Public Enumerations
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

	public class UsersItem : UserItem
	{
		#region Constructors
		internal UsersItem(UserItem baseUser, UserFlags flags, string? gender, string? token)
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
			this.Flags = flags;
			this.Gender = gender;
			this.Token = token;
		}
		#endregion

		#region Public Properties
		public UserFlags Flags { get; }

		public string? Gender { get; }

		public string? Token { get; }
		#endregion
	}
}