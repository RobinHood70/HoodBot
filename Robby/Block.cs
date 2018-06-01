namespace RobinHood70.Robby
{
	using System;
	using RobinHood70.WikiCommon;

	/// <summary>Stores all information about a wiki block.</summary>
	public class Block
	{
		internal Block(string user, string by, string reason, DateTime startTime, DateTime expiry, BlockFlags flags, bool automatic)
		{
			this.Automatic = automatic;
			this.BlockedBy = by;
			this.Expiry = expiry;
			this.Flags = flags;
			this.Reason = reason;
			this.StartTime = startTime;
			this.User = user;
		}

		/// <summary>Gets a value indicating whether whether the block was made automatically by the wiki software.</summary>
		public bool Automatic { get; }

		/// <summary>Gets the blocking user.</summary>
		public string BlockedBy { get; }

		/// <summary>Gets the date and time the block expires. DateTime.Max is used to represent an indefinite block.</summary>
		public DateTime Expiry { get; }

		/// <summary>Gets the block flags.</summary>
		public BlockFlags Flags { get; }

		/// <summary>Gets the reason the user was blocked.</summary>
		public string Reason { get; }

		/// <summary>Gets the time when the block was placed.</summary>
		public DateTime StartTime { get; }

		/// <summary>Gets the user who was blocked. DateTime.Min is used to represent unknown start times (usually only on very old or damaged wikis).</summary>
		public string User { get; }
	}
}
