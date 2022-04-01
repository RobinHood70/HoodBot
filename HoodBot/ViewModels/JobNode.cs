namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Models;

	// Parts of this class taken from https://www.codeproject.com/Articles/28306/Working-with-Checkboxes-in-the-WPF-TreeView
	public sealed class JobNode : TreeNode
	{
		#region Constructors
		public JobNode(TreeNode parent, JobInfo jobInfo)
			: base(parent, jobInfo.NotNull().Name)
		{
			this.JobInfo = jobInfo;
		}
		#endregion

		#region Public Properties
		public JobInfo JobInfo { get; }
		#endregion

		#region Public Static Methods
		public static TreeNode Populate()
		{
			var jobList = JobInfo.GetAllJobs();
			var jobGroups = MoveJobsIntoGroups(jobList);
			TreeNode root = new(null, "<Root>");
			foreach (var jobGroup in jobGroups)
			{
				TreeNode groupNode;
				if (jobGroup.Key.Length == 0)
				{
					groupNode = root;
				}
				else
				{
					groupNode = new TreeNode(root, jobGroup.Key);
					root.AddChild(groupNode);
				}

				foreach (var job in jobGroup.Value)
				{
					var child = job.Groups.Count == 0 ? new JobNode(groupNode, job) : new JobNode(groupNode, job);
					groupNode.AddChild(child);
				}
			}

			root.Sort();
			return root;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.DisplayText;
		#endregion

		#region Private Static Methods
		private static Dictionary<string, List<JobInfo>> MoveJobsIntoGroups(IEnumerable<JobInfo> jobList)
		{
			Dictionary<string, List<JobInfo>> retval = new(StringComparer.Ordinal);
			foreach (var job in jobList)
			{
				var jobGroups = job.Groups.Count == 0 ? new string[] { string.Empty } : job.Groups;
				foreach (var groupName in jobGroups)
				{
					var name = groupName.Length == 0 ? string.Empty : groupName;
					if (!retval.TryGetValue(name, out var children))
					{
						children = new List<JobInfo>();
						retval.Add(name, children);
					}

					children.Add(job);
				}
			}

			return retval;
		}
		#endregion
	}
}