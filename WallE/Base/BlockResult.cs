#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base;

using System;
using RobinHood70.WikiCommon;

public class BlockResult
{
	#region Constructors
	internal BlockResult(string user, long userId, string reason, DateTime? expiry, long id, BlockFlags flags, bool watchUser)
	{
		this.User = user;
		this.UserId = userId;
		this.Reason = reason;
		this.Expiry = expiry;
		this.Id = id;
		this.Flags = flags;
		this.WatchUser = watchUser;
	}
	#endregion

	#region Public Properties
	public DateTime? Expiry { get; }

	public BlockFlags Flags { get; }

	public long Id { get; }

	public string Reason { get; }

	public string User { get; }

	public long UserId { get; }

	public bool WatchUser { get; }
	#endregion

	#region Public Override Methods
	public override string ToString() => this.User;
	#endregion
}