namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	public sealed class TreeViewGroupedComparer : IComparer<TreeNode>, IComparer
	{
		#region Constructors
		private TreeViewGroupedComparer()
		{
		}
		#endregion

		#region Public Static Properties
		public static TreeViewGroupedComparer Instance { get; } = new TreeViewGroupedComparer();
		#endregion

		#region Public Methods
		public int Compare(TreeNode? x, TreeNode? y)
		{
			var retval = Globals.NullComparer(x, y) ?? x!.IsFolder.CompareTo(y!.IsFolder);
			return retval == 0
				? string.Compare(x!.DisplayText, y!.DisplayText, StringComparison.CurrentCulture)
				: retval;
		}

		int IComparer.Compare(object? x, object? y) => this.Compare(x as TreeNode, y as TreeNode);
		#endregion
	}
}
