namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Models;

	// Parts of this class taken from https://www.codeproject.com/Articles/28306/Working-with-Checkboxes-in-the-WPF-TreeView
	public sealed class JobNode(TreeNode? parent, JobInfo jobInfo) : TreeNode(parent, jobInfo.Name)
	{
		#region Public Properties
		public JobInfo JobInfo { get; } = jobInfo;
		#endregion

		#region Public Static Methods
		public static TreeNode Populate()
		{
			var jobList = JobInfo.GetAllJobs();
			var jobGroups = MoveJobsIntoGroups(jobList);
			var root = new TreeNode(null, "<Root>");
			foreach (var jobGroup in jobGroups)
			{
				TreeNode group;
				if (jobGroup.Key.Length == 0)
				{
					group = root;
				}
				else
				{
					group = new TreeNode(root, jobGroup.Key);
					root.AddChild(group);
				}

				foreach (var job in jobGroup.Value)
				{
					group.AddChild(new JobNode(group, job));
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
				var jobGroups = job.Groups.Count == 0 ? [string.Empty] : job.Groups;
				foreach (var groupName in jobGroups)
				{
					var name = groupName.Length == 0 ? string.Empty : groupName;
					if (!retval.TryGetValue(name, out var children))
					{
						children = [];
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