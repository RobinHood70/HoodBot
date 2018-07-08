namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using static RobinHood70.WikiCommon.Globals;

	public sealed class JobNode : Notifier, IComparable<JobNode>, IEquatable<JobNode>
	{
		#region Fields
		private bool? isChecked;
		#endregion

		#region Constructors
		public JobNode(string name, ConstructorInfo constructor)
			: this(name, constructor, null)
		{
		}

		public JobNode(string name, ConstructorInfo constructor, JobNode parent)
		{
			ThrowNull(name, nameof(name));
			this.Constructor = constructor;
			this.Name = name;
			this.Parent = parent;
			this.isChecked = false;
		}

		public JobNode(string name, ConstructorInfo constructor, JobNode parent, string firstChild)
			: this(name, null, parent) => this.Children = new SortedSet<JobNode>
			{
				new JobNode(firstChild, constructor, this)
			};
		#endregion

		#region Public Properties
		public ConstructorInfo Constructor { get; }

		public bool? IsChecked
		{
			get => this.isChecked;
			set
			{
				this.Set(ref this.isChecked, value, nameof(this.IsChecked));
				if (value == null)
				{
					if (this.Children?.Count > 0)
					{
						var falseCount = 0;
						var trueCount = 0;
						foreach (var child in this.Children)
						{
							if (child.IsChecked.HasValue)
							{
								if (child.IsChecked.Value)
								{
									trueCount++;
								}
								else
								{
									falseCount++;
								}
							}
						}

						if (trueCount == this.Children.Count)
						{
							this.Set(ref this.isChecked, true, nameof(this.IsChecked));
						}
						else if (falseCount == this.Children.Count)
						{
							this.Set(ref this.isChecked, false, nameof(this.IsChecked));
						}
					}
				}
				else
				{
					if (this.Children?.Count > 0)
					{
						foreach (var child in this.Children)
						{
							child.IsChecked = value;
						}
					}

					if (this.Parent != null)
					{
						this.Parent.IsChecked = null;
					}
				}
			}
		}

		public ICollection<JobNode> Children { get; }

		public string Name { get; }

		public Dictionary<string, object> Parameters { get; private set; }

		public JobNode Parent { get; }
		#endregion

		#region Public Operators
		public static bool operator ==(JobNode left, JobNode right) =>
			ReferenceEquals(left, right) ? true :
			left is null ? false :
			left.Equals(right);

		public static bool operator !=(JobNode left, JobNode right) => !(left == right);

		public static bool operator <(JobNode left, JobNode right) =>
			ReferenceEquals(left, right) ? false :
			left is null ? true :
			left.CompareTo(right) == -1;

		public static bool operator <=(JobNode left, JobNode right) =>
			ReferenceEquals(left, right) ? true :
			left is null ? true :
			left.CompareTo(right) != 1;

		public static bool operator >(JobNode left, JobNode right) =>
			ReferenceEquals(left, right) ? false :
			left is null ? false :
			left.CompareTo(right) == 1;

		public static bool operator >=(JobNode left, JobNode right) =>
			ReferenceEquals(left, right) ? true :
			left is null ? false :
			left.CompareTo(right) != -1;
		#endregion

		#region Public Methods
		public int CompareTo(JobNode other) =>
			other is null || (this.Children == null && other.Children?.Count > 0) ? 1 :
			other.Children is null && this.Children?.Count > 0 ? -1 :
			string.Compare(this.Name, other.Name, StringComparison.Ordinal);

		public bool Equals(JobNode other)
		{
			if (other is null)
			{
				return false;
			}

			if (this.Name != other.Name || this.Parent != other.Parent)
			{
				return false;
			}

			if (this.Children == null)
			{
				return other.Children == null;
			}

			var childSet = new HashSet<JobNode>(this.Children);
			return childSet.SetEquals(new HashSet<JobNode>(other.Children));
		}

		public void InitializeParameters() => this.Parameters = new Dictionary<string, object>();
		#endregion

		#region Public Override Methods
		public override bool Equals(object obj) => ReferenceEquals(this, obj) || this.Equals(obj as JobNode);

		public override int GetHashCode() => CompositeHashCode(this.Parent, this.Name, this.Children);
		#endregion
	}
}