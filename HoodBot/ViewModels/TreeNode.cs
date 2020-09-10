namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using GalaSoft.MvvmLight;
	using RobinHood70.CommonCode;
	using static RobinHood70.CommonCode.Globals;

	public class TreeNode : ObservableObject
	{
		#region Fields
		private bool? isChecked = false;
		private bool isSelected;
		private TreeNode? selectedItem;
		#endregion

		#region Constructors
		public TreeNode(TreeNode? parent, string displayText, IReadOnlyCollection<TreeNode>? children)
		{
			ThrowNull(displayText, nameof(displayText));
			this.Parent = parent;
			this.DisplayText = displayText;
			this.Children = children;
		}
		#endregion

		#region Public Events
		public event StrongEventHandler<TreeNode, SelectedItemChangedEventArgs>? SelectionChanged;
		#endregion

		#region Public Properties
		public IReadOnlyCollection<TreeNode>? Children { get; }

		public string DisplayText { get; }

		public bool? IsChecked
		{
			get => this.isChecked;
			set
			{
				if (this.Set(ref this.isChecked, value))
				{
					var selected = this.SelectedItem;
					if (selected != null)
					{
						selected.IsSelected = false;
					}

					if (value != null)
					{
						this.OnSelectionChange(new SelectedItemChangedEventArgs(this, value.Value));
						if (this.Children != null)
						{
							foreach (var c in this.Children)
							{
								c.IsChecked = value;
							}
						}
					}

					this.Parent?.RecheckValue();
				}
			}
		}

		public bool IsSelected
		{
			get => this.isSelected;
			set
			{
				if (this.isSelected != value)
				{
					this.Set(ref this.isSelected, value);
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
		public void AddChild(TreeNode child)
		{
			ThrowNull(child, nameof(child));
			if (this.Children is ICollection<TreeNode> editable)
			{
				editable.Add(child);
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public IEnumerable<TreeNode> CheckedChildren()
		{
			if (this.IsChecked != false)
			{
				if (this.Children == null)
				{
					yield return this;
				}
				else
				{
					foreach (var node in this.Children)
					{
						foreach (var item in node.CheckedChildren())
						{
							yield return item;
						}
					}
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
		#endregion

		#region Public Override Methods
		public override int GetHashCode() => HashCode.Combine(this.Parent, this.DisplayText);

		public override string ToString() => this.DisplayText;
		#endregion

		#region Protected Methods
		protected void OnSelectionChange(SelectedItemChangedEventArgs eventArgs)
		{
			ThrowNull(eventArgs, nameof(eventArgs));
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
			if (this.Children != null)
			{
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
			}

			return state;
		}
		#endregion
	}
}
