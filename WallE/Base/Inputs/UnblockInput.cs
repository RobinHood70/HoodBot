#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class UnblockInput
	{
		#region Constructors
		public UnblockInput(long blockId) => this.Id = blockId;

		public UnblockInput(string user) => this.User = user;

		private UnblockInput()
		{
		}
		#endregion

		#region Public Properties
		public long Id { get; }

		public string? Reason { get; set; }

		public IEnumerable<string>? Tags { get; set; }

		public string? Token { get; set; }

		public string? User { get; }

		public long UserId { get; private set; }
		#endregion

		#region Static Methods
		public static UnblockInput FromUserId(long userId) => new() { UserId = userId };
		#endregion
	}
}
