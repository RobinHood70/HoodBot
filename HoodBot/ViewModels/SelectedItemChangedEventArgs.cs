namespace RobinHood70.HoodBot.ViewModels
{
	using System;

	public class SelectedItemChangedEventArgs : EventArgs
	{
		public SelectedItemChangedEventArgs(TreeNode node, bool selected)
		{
			ArgumentNullException.ThrowIfNull(node);
			this.Node = node;
			this.Selected = selected;
		}

		public TreeNode Node { get; }

		public bool Selected { get; }
	}
}