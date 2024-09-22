#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System.Collections.Generic;

	public class ChangeableGroupsInfo
	{
		#region Constructors
		internal ChangeableGroupsInfo(IReadOnlyList<string> add, IReadOnlyList<string> addSelf, IReadOnlyList<string> remove, IReadOnlyList<string> removeSelf)
		{
			this.Add = add;
			this.AddSelf = addSelf;
			this.Remove = remove;
			this.RemoveSelf = removeSelf;
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<string> Add { get; }

		public IReadOnlyList<string> AddSelf { get; }

		public IReadOnlyList<string> Remove { get; }

		public IReadOnlyList<string> RemoveSelf { get; }
		#endregion
	}
}