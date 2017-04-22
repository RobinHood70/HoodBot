#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class UserGroupsItem
	{
		#region Public Properties
		public IReadOnlyList<string> Add { get; set; }

		public IReadOnlyList<string> AddSelf { get; set; }

		public string Name { get; set; }

		public long Number { get; set; }

		public IReadOnlyList<string> Remove { get; set; }

		public IReadOnlyList<string> RemoveSelf { get; set; }

		public IReadOnlyList<string> Rights { get; set; }
		#endregion
	}
}
