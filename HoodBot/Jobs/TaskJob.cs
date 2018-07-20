namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;

	public abstract class TaskJob : WikiJob
	{
		#region Fields
		private readonly Progress<double> taskProgressIntercept = new Progress<double>();
		#endregion

		#region Constructors
		protected TaskJob(Site site, AsyncInfo asyncInfo, params WikiTask[] tasks)
			: base(site, asyncInfo)
		{
			this.taskProgressIntercept.ProgressChanged += this.TaskProgressIntercept_ProgressChanged;
			if (tasks != null)
			{
				foreach (var task in tasks)
				{
					this.Tasks.Add(task);
				}
			}
		}
		#endregion

		#region Destructors
		~TaskJob() => this.taskProgressIntercept.ProgressChanged -= this.TaskProgressIntercept_ProgressChanged;
		#endregion

		#region Protected Properties
		protected IList<WikiTask> Tasks { get; } = new List<WikiTask>();
		#endregion

		#region Public Methods
		protected sealed override void Main()
		{
			this.ProgressMaximum = this.Tasks.Count;
			foreach (var task in this.Tasks)
			{
				var sw = new Stopwatch();
				sw.Start();

				task.SetAsyncInfoWithIntercept(this.taskProgressIntercept);
				task.Execute();
				this.IncrementProgress();

				sw.Stop();
				Debug.WriteLine($"{task.GetType().Name}: {sw.ElapsedMilliseconds}");
			}
		}
		#endregion

		#region Event Handlers
		private void TaskProgressIntercept_ProgressChanged(object sender, double e) => this.AsyncInfo.ProgressMonitor.Report((this.Progress + e) / this.ProgressMaximum);
		#endregion
	}
}