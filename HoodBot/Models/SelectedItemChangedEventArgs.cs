namespace RobinHood70.HoodBot.Models
{
	using System;
	using static RobinHood70.CommonCode.Globals;

	public class SelectedItemChangedEventArgs : EventArgs
	{
		public SelectedItemChangedEventArgs(TreeNode node, bool selected)
		{
			ThrowNull(node, nameof(node));
			this.Node = node;
			this.Selected = selected;
		}

		public TreeNode Node { get; }

		public bool Selected { get; }
	}
}