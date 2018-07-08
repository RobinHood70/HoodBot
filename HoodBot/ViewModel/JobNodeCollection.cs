namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Reflection;
	using RobinHood70.HoodBot.Jobs;
	using RobinHood70.HoodBot.Jobs.Design;

	public class JobNodeCollection : KeyedCollection<string, JobNode>
	{
		public JobNodeCollection()
		{
			var wikiJobType = typeof(WikiJob);
			var allNodes = new HashSet<JobNode>();
			foreach (var type in Assembly.GetCallingAssembly().GetTypes())
			{
				if (type.IsSubclassOf(wikiJobType))
				{
					foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
					{
						this.AddConstructor(constructor, allNodes);
					}
				}
			}

			(this.Items as List<JobNode>).Sort();
		}

		#region Public Methods
		public IEnumerable<JobNode> GetCheckedJobs() => GetCheckedJobs(this);

		public bool TryGetValue(string key, out JobNode item)
		{
			if (this.Dictionary != null)
			{
				return this.Dictionary.TryGetValue(key, out item);
			}

			foreach (var testItem in this)
			{
				if (this.GetKeyForItem(testItem) == key)
				{
					item = testItem;
					return true;
				}
			}

			item = null;
			return false;
		}
		#endregion

		#region Protected Override Methods
		protected override string GetKeyForItem(JobNode item) => item?.Name;
		#endregion

		#region Private Static Methods
		private static void CheckAndAdd(ICollection<JobNode> jobs, JobNode jobNode, HashSet<JobNode> allNodes)
		{
			Debug.WriteLine($"Parent: {jobNode.Parent?.Name}, Name: {jobNode.Name}, Constructor: {jobNode.Constructor}, Children Count: {jobNode.Children?.Count}");
			if (allNodes.Add(jobNode))
			{
				jobs.Add(jobNode);
			}
			else
			{
				throw new InvalidOperationException($"Job {jobNode.Name} has duplicate group {jobNode.Parent?.Name ?? "<Main List>"}.");
			}
		}

		private static IEnumerable<JobNode> GetCheckedJobs(ICollection<JobNode> jobs)
		{
			foreach (var job in jobs)
			{
				if (job.Children != null)
				{
					foreach (var childJob in GetCheckedJobs(job.Children))
					{
						yield return childJob;
					}
				}

				if (job.IsChecked == true && job.Constructor != null)
				{
					yield return job;
				}
			}
		}
		#endregion

		#region Private Methods
		private void AddConstructor(ConstructorInfo constructor, HashSet<JobNode> allNodes)
		{
			var jobInfo = constructor.GetCustomAttribute<JobInfoAttribute>();
			if (jobInfo == null)
			{
				return;
			}

			if (jobInfo.Groups == null)
			{
				CheckAndAdd(this, new JobNode(jobInfo.Name, constructor), allNodes);
				return;
			}

			foreach (var group in jobInfo.Groups)
			{
				if (string.IsNullOrEmpty(group))
				{
					CheckAndAdd(this, new JobNode(jobInfo.Name, constructor), allNodes);
				}
				else
				{
					// For now, this only expects a single level of children, but it was designed to support a full tree if needed. Parsing code for group names with slashes/dots would need to be added here.
					if (!this.TryGetValue(group, out var node))
					{
						CheckAndAdd(this, new JobNode(group, constructor, null, jobInfo.Name), allNodes);
					}
					else
					{
						CheckAndAdd(node.Children, new JobNode(jobInfo.Name, constructor, node), allNodes);
					}
				}
			}
		}
		#endregion
	}
}
