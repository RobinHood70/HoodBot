namespace RobinHood70.Robby
{
	using System;
	using RobinHood70.WikiCommon;

	/// <summary>Stores all information about a wiki block.</summary>
	public class Block
	{
		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="Block"/> class.</summary>
		/// <param name="user">The blocked user.</param>
		/// <param name="by">Who the block was performed by.</param>
		/// <param name="reason">The reason for the block.</param>
		/// <param name="startTime">The start time of the block.</param>
		/// <param name="expiry">When the block expires.</param>
		/// <param name="flags">The block flags.</param>
		/// <param name="automatic">if set to <c>true</c>, indicates that this was an auto-block by the wiki itself.</param>
		protected internal Block(string user, string by, string reason, DateTime startTime, DateTime expiry, BlockFlags flags, bool automatic)
		{
			this.Automatic = automatic;
			this.BlockedBy = by;
			this.Expiry = expiry;
			this.Flags = flags;
			this.Reason = reason;
			this.StartTime = startTime;
			this.User = user;
		}
		#endregion

		/// <summary>Gets a value indicating whether whether the block was made automatically by the wiki software.</summary>
		/// <value><c>true</c> if it was an automatic block; otherwise, <c>false</c>.</value>
		public bool Automatic { get; }

		/// <summary>Gets the blocking user.</summary>
		/// <value>The user this user was blocked by.</value>
		public string BlockedBy { get; }

		/// <summary>Gets the date and time the block expires. DateTime.Max is used to represent an indefinite block.</summary>
		/// <value>The date and time the block expires.</value>
		public DateTime Expiry { get; }

		/// <summary>Gets the block flags.</summary>
		/// <value>The block flags.</value>
		public BlockFlags Flags { get; }

		/// <summary>Gets the reason the user was blocked.</summary>
		/// <value>The reason the user was blocked.</value>
		public string Reason { get; }

		/// <summary>Gets the time when the block was placed. DateTime.Min is used to represent unknown start times (usually only on very old or damaged wikis).</summary>
		/// <value>The start time of the block.</value>
		public DateTime StartTime { get; }

		/// <summary>Gets the user who was blocked.</summary>
		/// <value>The user who was blocked.</value>
		public string User { get; }
	}
}
