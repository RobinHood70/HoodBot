namespace RobinHood70.HoodBot.ViewModels;

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
	public int Compare(TreeNode? x, TreeNode? y) =>
		Globals.NullComparer(x, y) ??
		Globals.ChainedCompareTo(y!.IsFolder, x!.IsFolder) ??
		Globals.ChainedCompareTo(x!.DisplayText, y!.DisplayText) ?? 0;

	int IComparer.Compare(object? x, object? y) => this.Compare(x as TreeNode, y as TreeNode);
	#endregion
}