namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;

	public class MainViewModel : INotifyPropertyChanged
	{
		#region Fields
		private readonly IReadOnlyList<object> extraParameters = new List<object>();
		private DateTime? eta;
		private DateTime jobStarted;
		private double botPicOpacity = 1;
		private double overallProgress = 0;
		private double overallProgressMax = 1;
		private int completedLoops;
		private int completedTasks;
		private int numberOfLoops = 1;
		private int numberOfTasks = 1;
		#endregion

		#region Public Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region Public Properties
		public double BotPicOpacity
		{
			get => this.botPicOpacity;
			set => this.Set(ref this.botPicOpacity, value);
		}

		public DateTime? Eta => this.eta?.ToLocalTime();

		public double OverallProgress
		{
			get => this.overallProgress;
			set => this.Set(ref this.overallProgress, value);
		}

		public double OverallProgressMax
		{
			get => this.overallProgressMax;
			set => this.Set(ref this.overallProgressMax, value < 1 ? 1 : value);
		}

		public DateTime? UtcEta
		{
			get => this.eta;
			set
			{
				if (this.Set(ref this.eta, value))
				{
					this.OnPropertyChanged(nameof(this.Eta));
				}
			}
		}
		#endregion

		#region Public Methods
		public void EndJob()
		{
			if (this.completedTasks != this.numberOfTasks - 1)
			{
				Debug.WriteLine($"Warning: Last JobProgress did not end at JobProgressMax: {this.completedTasks + 1} / {this.numberOfTasks}");
			}

			this.completedTasks = 0;
			this.completedLoops = 0;
			this.numberOfTasks = 1;
			this.numberOfLoops = 1;
			this.OverallProgress = 0;
			this.UtcEta = null;
		}

		public void IncrementTaskProgress()
		{
			this.completedLoops++;
			this.UpdateEta();
		}

		public void SetNumberOfLoops(int numLoops) => this.numberOfLoops = numLoops;

		public void SetNumberOfTasks(int numTasks) => this.numberOfTasks = numTasks;

		public void StartJob()
		{
			this.numberOfLoops = 1;
			this.completedTasks = -1;
			this.jobStarted = DateTime.UtcNow;
			this.OverallProgress = 0;
		}

		public void StartJob(int numTasks)
		{
			this.numberOfTasks = numTasks;
			this.StartJob();
		}

		public void StartTask()
		{
			if (this.completedLoops != this.numberOfLoops)
			{
				Debug.WriteLine("Warning: Last TaskProgress did not end at TaskProgressMax");
			}

			this.completedLoops = 0;
			this.completedTasks++;
		}

		public void StartTask(int taskProgressMax)
		{
			this.numberOfLoops = taskProgressMax;
			this.StartTask();
		}

		public void UpdateEta()
		{
			if (this.completedTasks > this.numberOfTasks)
			{
				Debug.WriteLine("Warning: JobProgress exceeds JobProgressMax - ignored");
				return;
			}

			if (this.completedLoops > this.numberOfLoops)
			{
				Debug.WriteLine("Warning: TaskProgress exceeds TaskProgressMax - ignored");
				return;
			}

			var progress = this.numberOfLoops * this.completedTasks + this.completedLoops;
			if (progress > 0)
			{
				var progressMax = this.numberOfTasks * this.numberOfLoops;
				var timeDiff = DateTime.UtcNow - this.jobStarted;
				var completionTime = TimeSpan.FromTicks(timeDiff.Ticks * progressMax / progress);

				this.OverallProgress = progress;
				this.OverallProgressMax = this.numberOfTasks * this.numberOfLoops;

				if (timeDiff.TotalSeconds >= 5)
				{
					this.UtcEta = this.jobStarted + completionTime;
				}
			}
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		#endregion

		#region Private Methods
		private bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			var retval = !EqualityComparer<T>.Default.Equals(field, value);
			if (retval)
			{
				field = value;
				this.OnPropertyChanged(propertyName);
			}

			return retval;
		}
		#endregion
	}
}