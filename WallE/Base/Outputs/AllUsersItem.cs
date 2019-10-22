#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class AllUsersItem : InternalUserItem
	{
		#region Constructors
		internal AllUsersItem(long userId, string name, int recentActions)
			: base(userId, name) => this.RecentActions = recentActions;
		#endregion

		#region Public Properties
		public int RecentActions { get; }
		#endregion
	}
}
