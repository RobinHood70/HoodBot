#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;

	public class BlockInput
	{
		#region Constructors
		public BlockInput(string user) => this.User = user;

		public BlockInput(long userId) => this.UserId = userId;
		#endregion

		#region Public Properties
		public bool AllowUserTalk { get; set; }

		public bool AnonymousOnly { get; set; }

		public bool AutoBlock { get; set; }

		public DateTime? Expiry { get; set; }

		public string ExpiryRelative { get; set; }

		public bool HideName { get; set; }

		public bool NoCreate { get; set; }

		public bool NoEmail { get; set; }

		public string Reason { get; set; }

		public bool Reblock { get; set; }

		public string Token { get; set; }

		public string User { get; }

		public long UserId { get; }

		public bool WatchUser { get; set; }
		#endregion
	}
}
