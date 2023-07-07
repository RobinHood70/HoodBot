namespace RobinHood70.HoodBot.ViewModels
{
	using System.Collections.Generic;
	using CommunityToolkit.Mvvm.ComponentModel;
	using RobinHood70.CommonCode;

	public class TreeNode : ObservableObject
	{
		#region Fields
		private readonly List<TreeNode> children = new();
		private bool? isChecked = false;
		private bool isSelected;
		private TreeNode? selectedItem;
		#endregion

		#region Constructors
		public TreeNode(TreeNode? parent, string displayText)
		{
			this.Parent = parent;
			this.DisplayText = displayText.NotNull();
		}
		#endregion

		#region Public Events
		public event StrongEventHandler<TreeNode, SelectedItemChangedEventArgs>? SelectionChanged;
		#endregion

		#region Public Properties
		public IReadOnlyCollection<TreeNode> Children => this.children;

		public string DisplayText { get; }

		public bool? IsChecked
		{
			get => this.isChecked;
			set
			{
				if (this.SetProperty(ref this.isChecked, value))
				{
					var selected = this.SelectedItem;
					if (selected != null)
					{
						selected.IsSelected = false;
					}

					if (value != null)
					{
						this.OnSelectionChange(new SelectedItemChangedEventArgs(this, value.Value));
						foreach (var c in this.Children)
						{
							c.IsChecked = value;
						}
					}

					this.Parent?.RecheckValue();
				}
			}
		}

		public bool IsFolder => this.Children.Count != 0;

		public bool IsSelected
		{
			get => this.isSelected;
			set
			{
				if (this.isSelected != value)
				{
					this.SetProperty(ref this.isSelected, value);
					this.Root.selectedItem = value ? this : null;
					this.OnSelectionChange(new SelectedItemChangedEventArgs(this, value));
				}
			}
		}

		public TreeNode? SelectedItem => this.Parent == null ? this.selectedItem : this.Root.SelectedItem;
		#endregion

		#region Protected Properties
		protected TreeNode? Parent { get; }

		protected TreeNode Root => this.Parent?.Root ?? this;
		#endregion

		#region Public Methods
		public void AddChild(TreeNode child) => this.children.Add(child.NotNull());

		public IEnumerable<TreeNode> CheckedChildren()
		{
			if (this.IsChecked != false)
			{
				if (this.IsFolder)
				{
					foreach (var node in this.Children)
					{
						foreach (var item in node.CheckedChildren())
						{
							yield return item;
						}
					}
				}
				else
				{
					yield return this;
				}
			}
		}

		public IEnumerable<T> CheckedChildren<T>()
			where T : TreeNode
		{
			foreach (var node in this.CheckedChildren())
			{
				yield return (T)node;
			}
		}

		public void RecheckValue()
		{
			var value = this.ChildrenCheckState();
			this.IsChecked = value;
		}

		public void Sort() => this.Sort(TreeViewGroupedComparer.Instance, true);

		public void Sort(IComparer<TreeNode> comparer, bool recursive)
		{
			if (this.children.Count == 0)
			{
				return;
			}

			this.children.Sort(comparer);
			if (recursive)
			{
				foreach (var child in this.children)
				{
					child.Sort(comparer, recursive);
				}
			}
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.DisplayText;
		#endregion

		#region Protected Methods
		protected void OnSelectionChange(SelectedItemChangedEventArgs eventArgs)
		{
			eventArgs.ThrowNull();
			if (this.Parent == null)
			{
				this.SelectionChanged?.Invoke(this, eventArgs);
			}
			else
			{
				this.Root.OnSelectionChange(eventArgs);
			}
		}
		#endregion

		#region Private Methods
		private bool? ChildrenCheckState()
		{
			bool? state = null;
			foreach (var child in this.Children)
			{
				if (child.IsChecked == null)
				{
					state = null;
					break;
				}

				if (state == null)
				{
					state = child.IsChecked;
				}
				else if (state.Value != child.IsChecked)
				{
					state = null;
					break;
				}
			}

			return state;
		}
		#endregion
	}
}
