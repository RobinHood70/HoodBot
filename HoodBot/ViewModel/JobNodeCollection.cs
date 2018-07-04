namespace RobinHood70.HoodBot.ViewModel
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using RobinHood70.HoodBot.Jobs;

	public class JobNodeCollection : KeyedCollection<string, JobNode>
	{
		public JobNodeCollection()
		{
			foreach (var jobInfo in JobInfoAttribute.FindAll())
			{
				if (jobInfo.Groups == null)
				{
					this.Add(new JobNode(jobInfo.Name));
				}
				else
				{
					foreach (var group in jobInfo.Groups)
					{
						if (string.IsNullOrEmpty(group))
						{
							this.Add(new JobNode(jobInfo.Name));
						}
						else
						{
							// For now, this only supports a single level of children, but it was designed to support a full tree if needed. Parsing code for group names with slashes/dots would need to be added here.
							if (!this.TryGetValue(group, out var node))
							{
								this.Add(new JobNode(group, null, jobInfo.Name));
							}
							else
							{
								node.Children.Add(new JobNode(jobInfo.Name, node));
							}
						}
					}
				}
			}

			this.Sort();
		}

		#region Public Methods
		public void Sort()
		{
			if (this.Items is List<JobNode> items)
			{
				items.Sort();
			}
		}

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
	}
}
