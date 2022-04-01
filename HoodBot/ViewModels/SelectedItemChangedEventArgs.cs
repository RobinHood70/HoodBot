namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using RobinHood70.CommonCode;

	public class SelectedItemChangedEventArgs : EventArgs
	{
		public SelectedItemChangedEventArgs(TreeNode node, bool selected)
		{
			this.Node = node.NotNull();
			this.Selected = selected;
		}

		public TreeNode Node { get; }

		public bool Selected { get; }
	}
}