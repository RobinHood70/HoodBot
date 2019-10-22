#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class UserRightsInput
	{
		#region Constructors
		public UserRightsInput(string user) => this.User = user;

		public UserRightsInput(long userId) => this.UserId = userId;
		#endregion

		#region Public Properties
		public IEnumerable<string>? Add { get; set; }

		public string? Reason { get; set; }

		public IEnumerable<string>? Remove { get; set; }

		public string? Token { get; set; }

		public string? User { get; }

		public long UserId { get; }
		#endregion
	}
}
