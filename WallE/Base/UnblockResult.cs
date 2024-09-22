#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class UnblockResult
	{
		#region Constructors
		internal UnblockResult(long id, string user, long userId, string? reason)
		{
			this.Id = id;
			this.User = user;
			this.UserId = userId;
			this.Reason = reason;
		}
		#endregion

		#region Public Properties
		public long Id { get; }

		public string? Reason { get; }

		public string User { get; }

		public long UserId { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.User;
		#endregion
	}
}