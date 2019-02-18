namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using static RobinHood70.WikiCommon.Extensions;
	using static RobinHood70.WikiCommon.Globals;

	public abstract class WikiJob : WikiRunner
	{
		#region Fields
		private readonly Progress<double> taskProgressIntercept = new Progress<double>();
		#endregion

		#region Constructors
		protected WikiJob(Site site, AsyncInfo asyncInfo, params WikiTask[] tasks)
			: base(site)
		{
			ThrowNull(asyncInfo, nameof(asyncInfo));
			this.AsyncInfo = asyncInfo;
			this.taskProgressIntercept.ProgressChanged += this.TaskProgressIntercept_ProgressChanged;
			this.Tasks.AddRange(tasks);
		}
		#endregion

		#region Protected Properties
		protected IList<WikiTask> Tasks { get; } = new List<WikiTask>();
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.ProgressMaximum = this.Tasks.Count + 1;
			this.MainJob();
			this.Progress++;
			this.RunTasks();
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual void RunTasks()
		{
			foreach (var task in this.Tasks)
			{
				var sw = new Stopwatch();
				sw.Start();

				task.SetAsyncInfoWithIntercept(this.taskProgressIntercept);
				task.Execute();
				this.Progress++;

				sw.Stop();
				Debug.WriteLine($"{task.GetType().Name}: {sw.ElapsedMilliseconds}");
			}
		}
		#endregion

		#region Public Abstract Methods
		protected abstract void MainJob();
		#endregion

		#region Event Handlers
		private void TaskProgressIntercept_ProgressChanged(object sender, double e) => this.AsyncInfo.ProgressMonitor.Report((this.Progress + e) / this.ProgressMaximum);
		#endregion
	}
}