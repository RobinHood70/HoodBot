namespace RobinHood70.Robby.Pages
{
	using System;
	using RobinHood70.WallE.Base;

	#region Public Enumerations
	[Flags]
	public enum BlockFlags
	{
		None = BlockUserFlags.None,
		AccountCreationDisabled = BlockUserFlags.NoCreate,
		AnonymousOnly = BlockUserFlags.AnonymousOnly,
		AutoBlock = BlockUserFlags.AutoBlock,
		EmailDisabled = BlockUserFlags.NoEmail,
		UserTalk = BlockUserFlags.AllowUserTalk,
		Unknown = 0x40000000,
	}
	#endregion

	public class Block
	{
		public Block(string user, string by, string reason, DateTime startTime, DateTime expiry, BlockFlags flags, bool automatic)
		{
			this.Automatic = automatic;
			this.BlockedBy = by;
			this.Expiry = expiry;
			this.Flags = flags;
			this.Reason = reason;
			this.StartTime = startTime;
			this.User = user;
		}

		public bool Automatic { get; }

		public string BlockedBy { get; }

		public DateTime Expiry { get; }

		public BlockFlags Flags { get; }

		public string Reason { get; }

		public DateTime StartTime { get; }

		public string User { get; }
	}
}
