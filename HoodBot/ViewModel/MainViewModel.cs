namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Threading.Tasks;
	using RobinHood70.HoodBot.Jobs;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Eve;
	using static System.Environment;

	public class MainViewModel : Notifier
	{
		#region Private Constants
		private const string ContactInfo = "robinhood70@live.ca";
		#endregion

		#region Fields
		private readonly IMediaWikiClient client;
		private readonly string dataFolder;
		private readonly IReadOnlyList<object> extraParameters = new List<object>();

		private double botPicOpacity = 1;
		private int completedLoops;
		private int completedTasks;
		private WikiInfo currentItem;
		private DateTime? eta;
		private DateTime jobStarted;
		private int numberOfLoops = 1;
		private int numberOfTasks = 1;
		private double overallProgress;
		private double overallProgressMax = 1;
		private string password;
		private WikiInfo previousItem;
		private Site site;
		#endregion

		#region Constructors
		public MainViewModel()
		{
			this.dataFolder = Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.Create), nameof(HoodBot));
			this.client = new SimpleClient(ContactInfo, Path.Combine(this.dataFolder, "Cookies.dat"));
			this.KnownWikis = WikiList.Load(Path.Combine(this.dataFolder, "WikiList.json"));
			this.CurrentItem = this.KnownWikis.LastSelectedItem;
		}
		#endregion

		#region Destrctor
		~MainViewModel() => this.KnownWikis.Save();
		#endregion

		#region Public Properties
		public double BotPicOpacity
		{
			get => 1; // this.botPicOpacity;
			private set => this.Set(ref this.botPicOpacity, value, nameof(this.BotPicOpacity));
		}

		public WikiInfo CurrentItem
		{
			get => this.currentItem;
			set
			{
				if (this.Set(ref this.currentItem, value, nameof(this.CurrentItem)))
				{
					this.KnownWikis.UpdateLastSelected(value);
				}
			}
		}

		public RelayCommand EditWikiList => new RelayCommand(this.OpenEditWindow);

		public DateTime? Eta => this.eta?.ToLocalTime();

		public WikiList KnownWikis { get; }

		public double OverallProgress
		{
			get => this.overallProgress;
			private set => this.Set(ref this.overallProgress, value, nameof(this.OverallProgress));
		}

		public double OverallProgressMax
		{
			get => this.overallProgressMax;
			private set => this.Set(ref this.overallProgressMax, value < 1 ? 1 : value, nameof(this.OverallProgressMax));
		}

		public string Password
		{
			get => this.password;
			set => this.Set(ref this.password, value, nameof(this.Password));
		}

		public RelayCommand Play => new RelayCommand(this.ExecuteJobs);

		public DateTime? UtcEta
		{
			get => this.eta;
			private set
			{
				if (this.Set(ref this.eta, value, nameof(this.UtcEta)))
				{
					this.OnPropertyChanged(nameof(this.Eta));
				}
			}
		}
		#endregion

		#region Internal Properties
		public JobNodeCollection JobList { get; } = new JobNodeCollection();
		#endregion

		#region Public Methods
		public void SetBotPicOpacity(bool hasControls) => this.BotPicOpacity = hasControls ? 0.3 : 1;

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

		#region Private Methods
		private async void ExecuteJobs()
		{
			this.SetupWiki();
			var job = new OneJob(this.site);
			job.Completed += this.Job_Completed;
			job.ProgressChanged += this.Job_ProgressChanged;
			job.Started += this.Job_Started;
			job.TaskStarted += this.Job_TaskStarted;
			var task = Task.Run(() => { job.Execute(); });
			await task;
			job.TaskStarted -= this.Job_TaskStarted;
			job.Started -= this.Job_Started;
			job.ProgressChanged -= this.Job_ProgressChanged;
			job.Completed -= this.Job_Completed;
		}

		private void Job_Completed(WikiJob sender, EventArgs eventArgs)
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

		private void Job_ProgressChanged(WikiJob sender, Jobs.Tasks.ProgressEventArgs eventArgs)
		{
			this.completedLoops = eventArgs.Progress;
			this.numberOfLoops = eventArgs.ProgressMaximum;
			this.UpdateEta();
		}

		private void Job_Started(WikiJob sender, EventArgs eventArgs)
		{
			this.numberOfTasks = sender.Tasks.Count;
			this.numberOfLoops = 1;
			this.completedTasks = -1;
			this.jobStarted = DateTime.UtcNow;
			this.OverallProgress = 0;
		}

		private void Job_TaskStarted(WikiJob sender, Jobs.Tasks.TaskEventArgs eventArgs)
		{
			if (this.completedTasks >= 0 && this.completedLoops != this.numberOfLoops)
			{
				Debug.WriteLine("Warning: Last TaskProgress did not end at TaskProgressMax");
			}

			this.completedLoops = 0;
			this.completedTasks++;
		}

		private void OpenEditWindow()
		{
			var editWindow = new EditWikiList();
			var view = editWindow.DataContext as EditWindowViewModel;
			view.Client = this.client;
			view.KnownWikis = this.KnownWikis;
			view.CurrentItem = this.KnownWikis.LastSelectedItem;
			editWindow.ShowDialog();

			this.CurrentItem = this.KnownWikis.LastSelectedItem;
		}

		private void SetupWiki()
		{
			var wikiInfo = this.CurrentItem;
			if (wikiInfo == null)
			{
				throw new InvalidOperationException("No wiki has been selected.");
			}

			if (wikiInfo != this.previousItem)
			{
				this.previousItem = wikiInfo;
				var wal = new WikiAbstractionLayer(this.client, wikiInfo.Api);
				this.site = new Site(wal);
				this.site.Login(wikiInfo.UserName, this.Password ?? wikiInfo.Password);
			}
		}
		#endregion
	}
}