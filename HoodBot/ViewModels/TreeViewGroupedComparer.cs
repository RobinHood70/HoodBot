namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;

	public class TreeViewGroupedComparer : IComparer<TreeNode>
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
		public int Compare([AllowNull] TreeNode x, [AllowNull] TreeNode y)
		{
			if (x is null)
			{
				return y is null ? 0 : -1;
			}

			if (y is null)
			{
				return 1;
			}

			if (x.Children is null)
			{
				if (y.Children is object)
				{
					return 1;
				}
			}
			else
			{
				if (y.Children is null)
				{
					return -1;
				}
			}

			return string.Compare(x.DisplayText, y.DisplayText, StringComparison.Ordinal);
		}
	}
	#endregion
}
