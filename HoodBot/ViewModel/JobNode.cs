﻿namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Reflection;
	using RobinHood70.HoodBot.Jobs;
	using RobinHood70.HoodBot.Jobs.Design;
	using static RobinHood70.WikiCommon.Globals;

	public sealed class JobNode : Notifier, IComparable<JobNode>, IEquatable<JobNode>
	{
		#region Fields
		private bool? isChecked;
		#endregion

		#region Constructors
		public JobNode()
		{
			var wikiJobType = typeof(WikiJob);
			this.Children = new SortedSet<JobNode>();
			foreach (var type in Assembly.GetCallingAssembly().GetTypes())
			{
				if (type.IsSubclassOf(wikiJobType))
				{
					foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
					{
						this.ValidateAndAdd(constructor);
					}
				}
			}
		}

		public JobNode(JobNode parent, string groupName)
		{
			ThrowNull(parent, nameof(parent));
			ThrowNull(groupName, nameof(groupName));
			Debug.WriteLine($"Adding Group {groupName} to {parent.Name ?? "<ROOT>"}, Children Count: {parent.Children?.Count}");
			this.Children = new SortedSet<JobNode>();
			this.Name = groupName;
			this.Parent = parent;
			this.isChecked = false;
		}

		public JobNode(JobNode parent, ConstructorInfo constructor)
		{
			ThrowNull(parent, nameof(parent));
			ThrowNull(constructor, nameof(constructor));
			var jobName = constructor.GetCustomAttribute<JobInfoAttribute>().Name;
			var constructorName = constructor.DeclaringType.Name + constructor.ToString().Replace("Void .ctor", string.Empty).Replace("RobinHood70.Robby.", string.Empty).Replace("RobinHood70.HoodBot.Jobs.Design.", string.Empty);
			Debug.WriteLine($"Adding Job {jobName} with constructor {constructorName} to {parent.Name ?? "<ROOT>"}, Children Count: {parent.Children?.Count}");

			this.Constructor = constructor;
			this.Name = jobName;
			this.Parent = parent;
			this.isChecked = false;
		}
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

		public SortedSet<JobNode> Children { get; }

		public string Name { get; }

		public IReadOnlyList<ConstructorParameter> Parameters { get; private set; }

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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Longer operation than a simple fetch.")]
		public IEnumerable<JobNode> GetCheckedJobs()
		{
			if (this.Children != null)
			{
				foreach (var job in this.Children)
				{
					if (job.Constructor == null)
					{
						job.GetCheckedJobs();
					}
					else if (job.IsChecked == true)
					{
						yield return job;
					}
				}
			}
		}

		public void InitializeParameters()
		{
			var parameters = new List<ConstructorParameter>();
			var wantedParameters = this.Constructor.GetParameters();
			foreach (var parameter in wantedParameters)
			{
				var paramInfos = parameter.GetCustomAttributes(typeof(JobParameterAttribute), true);
				var paramType = parameter.ParameterType;
				var paramInfo = (paramInfos.Length == 1 ? paramInfos[0] : null) as JobParameterAttribute;
				switch (parameter.ParameterType.Name)
				{
					case "Site":
					case "AsyncInfo":
						break;
					default:
						object value = null;
						if (paramInfo?.DefaultValue != null)
						{
							value = paramInfo.DefaultValue;
						}
						else if (paramType.IsValueType)
						{
							value = Activator.CreateInstance(paramType);
						}

						parameters.Add(new ConstructorParameter(paramInfo?.Label, parameter, value));
						break;
				}
			}

			this.Parameters = parameters;
		}
		#endregion

		#region Public Override Methods
		public override bool Equals(object obj) => ReferenceEquals(this, obj) || this.Equals(obj as JobNode);

		public override int GetHashCode() => CompositeHashCode(this.Parent, this.Name, this.Children);
		#endregion

		#region Private Methods
		private void AddConstructor(ConstructorInfo constructor) => this.Children.Add(new JobNode(this, constructor));

		private void ValidateAndAdd(ConstructorInfo constructor)
		{
			var jobInfo = constructor.GetCustomAttribute<JobInfoAttribute>();
			if (jobInfo == null)
			{
				return;
			}

			var groups = jobInfo.Groups;
			if (groups == null)
			{
				this.AddConstructor(constructor);
				return;
			}

			foreach (var group in groups)
			{
				if (string.IsNullOrEmpty(group))
				{
					this.AddConstructor(constructor);
				}
				else
				{
					var addedToGroup = false;
					foreach (var child in this.Children)
					{
						if (child.Name == group)
						{
							child.AddConstructor(constructor);
							addedToGroup = true;
							break;
						}
					}

					if (!addedToGroup)
					{
						var child = new JobNode(this, group);
						this.Children.Add(child);
						child.AddConstructor(constructor);
					}
				}
			}
		}
		#endregion
	}
}