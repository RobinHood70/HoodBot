namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Models;
	using static RobinHood70.CommonCode.Globals;

	// Parts of this class taken from https://www.codeproject.com/Articles/28306/Working-with-Checkboxes-in-the-WPF-TreeView
	public sealed class JobNode : TreeNode
	{
		#region Constructors
		public JobNode(TreeNode parent, JobInfo jobInfo)
			: base(parent, (jobInfo ?? throw ArgumentNull(nameof(jobInfo))).Name, null) => this.JobInfo = jobInfo;
		#endregion

		#region Public Properties
		public JobInfo JobInfo { get; }
		#endregion

		#region Public Static Methods
		public static TreeNode Populate()
		{
			const string rootName = "<Root>";
			var groups = new Dictionary<string, TreeNode>(StringComparer.Ordinal);
			var jobList = JobInfo.GetAllJobs();
			var root = new TreeNode(null, rootName, new SortedSet<TreeNode>(TreeViewGroupedComparer.Instance));
			groups.Add(root.DisplayText, root);
			foreach (var job in jobList)
			{
				if (job.Groups.Count == 0)
				{
					root.AddChild(new JobNode(root, job));
				}

				foreach (var groupName in job.Groups)
				{
					var name = groupName.Length == 0 ? rootName : groupName;
					if (!groups.TryGetValue(name, out var group))
					{
						group = new TreeNode(root, name, new SortedSet<TreeNode>(TreeViewGroupedComparer.Instance));
						groups.Add(group.DisplayText, group);
						root.AddChild(group);
					}

					group.AddChild(new JobNode(group, job));
				}
			}

			return root;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.DisplayText;
		#endregion
	}
}