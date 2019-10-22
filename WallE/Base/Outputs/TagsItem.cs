#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	public class TagsItem
	{
		#region Constructors
		internal TagsItem(string name, string? description, string? displayName, int hitCount)
		{
			this.Name = name;
			this.Description = description;
			this.DisplayName = displayName;
			this.HitCount = hitCount;
		}
		#endregion

		#region Public Properties
		public string? Description { get; }

		public string? DisplayName { get; }

		public int HitCount { get; }

		public string Name { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion
	}
}
