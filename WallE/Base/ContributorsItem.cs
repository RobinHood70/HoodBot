#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class ContributorsItem(string name, long userId)
	{
		#region Public Properties
		public string Name => name;

		public long UserId => userId;
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}