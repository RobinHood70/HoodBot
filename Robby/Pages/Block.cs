namespace RobinHood70.Robby.Pages
{
	using System;
	using RobinHood70.WallE.Base;

	#region Public Enumerations
	[Flags]
	public enum BlockFlags
	{
		Unknown = -1,
		None = BlockUserFlags.None,
		AccountCreationDisabled = BlockUserFlags.NoCreate,
		AnonymousOnly = BlockUserFlags.AnonymousOnly,
		AutoBlock = BlockUserFlags.AutoBlock,
		EmailDisabled = BlockUserFlags.NoEmail,
		UserTalk = BlockUserFlags.AllowUserTalk,
	}
	#endregion

	public class Block
	{
		public Block(string user, string by, string reason, DateTime startTime, DateTime expiry, BlockFlags flags)
		{
			this.BlockedBy = by;
			this.Expiry = expiry;
			this.Flags = flags;
			this.Reason = reason;
			this.StartTime = startTime;
			this.User = user;
		}

		public string BlockedBy { get; }

		public DateTime Expiry { get; }

		public BlockFlags Flags { get; }

		public string Reason { get; }

		public DateTime StartTime { get; }

		public string User { get; }
	}
}
