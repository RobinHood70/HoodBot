#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class UserRightsResult
	{
		#region Constructors
		internal UserRightsResult(string user, long userId, IReadOnlyList<string> added, IReadOnlyList<string> removed)
		{
			this.User = user;
			this.UserId = userId;
			this.Added = added;
			this.Removed = removed;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> Added { get; }

		public IReadOnlyList<string> Removed { get; }

		public string User { get; }

		public long UserId { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.User;
		#endregion
	}
}
