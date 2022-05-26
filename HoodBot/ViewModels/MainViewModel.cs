namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Media;
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.Command;
	using RobinHood70.HoodBot.Jobs;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Loggers;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.HoodBot.Properties;
	using RobinHood70.HoodBot.Views;
	using RobinHood70.HoodBotPlugins;
	using static System.Environment;

	public class MainViewModel : ViewModelBase
	{
		#region Static Fields
		private static readonly Brush ProgressBarGreen = new SolidColorBrush(Color.FromArgb(255, 6, 176, 37));
		private static readonly Brush ProgressBarYellow = new SolidColorBrush(Color.FromArgb(255, 255, 240, 0));
		#endregion

		#region Fields
		private readonly CancellationTokenSource canceller = new();
		private readonly IDiffViewer? diffViewer;
		private readonly PauseTokenSource pauser = new();
		private readonly IProgress<double> progressMonitor;
		private readonly IProgress<string> statusMonitor;

		private double completedJobs;
		private bool editingEnabled;
		private DateTime? eta;
		private bool executing;
		private bool jobParametersEnabled;
		private Visibility jobParameterVisibility = Visibility.Hidden;
		private DateTime jobStarted;
		private double overallProgress;
		private double overallProgressMax = 1;
		private IParameterFetcher? parameterFetcher;
		private string? password;
		private Brush progressBarColor = ProgressBarGreen;
		private WikiInfoViewModel? selectedItem;
		private string status = string.Empty;
		private string? userName;
		#endregion

		#region Constructors
		public MainViewModel()
		{
			this.ShowDiffs = true;
			this.SelectedItem = App.UserSettings.GetCurrentItem();

			this.progressMonitor = new Progress<double>(this.ProgressChanged);
			this.statusMonitor = new Progress<string>(this.StatusWrite);

			var plugins = Plugins.Instance;
			this.diffViewer = plugins.DiffViewers["Internet Explorer"];

			this.JobTree.SelectionChanged += this.JobTree_OnSelectionChanged;
		}
		#endregion

		#region Public Commands
		public RelayCommand EditSettings => new(this.OpenEditWindow);

		public RelayCommand Play
		{
			get
			{
				async void ExecuteJobsAsync()
				{
					if (!this.executing && this.selectedItem is WikiInfoViewModel wikiInfo)
					{
						this.executing = true;
						this.StatusWriteLine("Initializing");
						var allJobsTimer = Stopwatch.StartNew();
						await this.ExecuteJobs(wikiInfo).ConfigureAwait(true);
						this.StatusWriteLine("Total time for last run: " + FormatTimeSpan(allJobsTimer.Elapsed));
						this.executing = false;
					}
				}

				return new(ExecuteJobsAsync);
			}
		}

		public RelayCommand Pause => new(this.PauseJobs);

		public RelayCommand Stop => new(this.CancelJobs);

		public RelayCommand Test => new(this.RunTest);
		#endregion

		#region Public Properties
		public bool EditingEnabled
		{
			get => this.editingEnabled;
			set => this.Set(ref this.editingEnabled, value, nameof(this.EditingEnabled));
		}

		public DateTime? Eta => this.eta?.ToLocalTime();

		public bool JobParametersEnabled
		{
			get => this.jobParametersEnabled;
			private set => this.Set(ref this.jobParametersEnabled, value, nameof(this.JobParametersEnabled));
		}

		public Visibility JobParameterVisibility
		{
			get => this.jobParameterVisibility;
			private set => this.Set(ref this.jobParameterVisibility, value, nameof(this.JobParameterVisibility));
		}

		public TreeNode JobTree { get; } = JobNode.Populate();

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

		public string? Password
		{
			get => this.password;
			set => this.Set(ref this.password, value, nameof(this.Password));
		}

		public Brush ProgressBarColor
		{
			get => this.progressBarColor;
			set => this.Set(ref this.progressBarColor, value, nameof(this.ProgressBarColor));
		}

		public WikiInfoViewModel? SelectedItem
		{
			get => this.selectedItem;
			set
			{
				if (value != null)
				{
					var userSettings = App.UserSettings;
					if (!string.Equals(userSettings.SelectedName, value.DisplayName, StringComparison.Ordinal))
					{
						userSettings.SelectedName = value.DisplayName;
					}
				}

				this.Set(ref this.selectedItem, value, nameof(this.SelectedItem));
			}
		}

		public bool ShowDiffs { get; }

		public string Status
		{
			get => this.status;
			set => this.Set(ref this.status, value ?? string.Empty, nameof(this.Status));
		}

		public UserSettings UserSettings { get; } = App.UserSettings;

		public string? UserName
		{
			get => this.userName;
			set => this.Set(ref this.userName, value, nameof(this.UserName));
		}

		public DateTime? UtcEta
		{
			get => this.eta;
			private set
			{
				if (this.Set(ref this.eta, value, nameof(this.UtcEta)))
				{
					this.RaisePropertyChanged(nameof(this.Eta));
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static string FormatTimeSpan(TimeSpan allJobsTimer) => allJobsTimer.ToString(@"h\h\ m\m\ s\.f\s", CultureInfo.CurrentCulture)
			.Replace("0h", string.Empty, StringComparison.Ordinal)
			.Replace(" 0m", string.Empty, StringComparison.Ordinal)
			.Replace(".0", string.Empty, StringComparison.Ordinal)
			.Replace(" 0s", string.Empty, StringComparison.Ordinal)
			.Trim();
		#endregion

		#region Private Methods
		private void CancelJobs()
		{
			this.canceller.Cancel();
			this.Reset();
			this.PauseJobs(isPaused: false);
		}

		private void ClearStatus() => this.Status = string.Empty; // TODO: Removed from Reset, so add to a button or maybe only on Play.

		private async Task ExecuteJobs(WikiInfoViewModel wikiInfo)
		{
			this.parameterFetcher?.SetParameters();

			List<JobInfo> jobList = new();
			foreach (var node in this.JobTree.CheckedChildren<JobNode>())
			{
				jobList.Add(node.JobInfo);
			}

			if (jobList.Count == 0)
			{
				this.executing = false;
				return;
			}

			this.ClearStatus();
			this.completedJobs = 0;
			this.OverallProgressMax = jobList.Count;

			var jobManager = new JobManager(wikiInfo.WikiInfo, this.progressMonitor, this.statusMonitor, this.pauser, this.canceller);
			try
			{
				jobManager.StartingJob += this.JobManager_StartingJob;
				jobManager.FinishedJob += this.JobManager_FinishedJob;
				jobManager.PagePreview += this.SitePagePreview;
				jobManager.Site.EditingEnabled = this.EditingEnabled;

				var loginName = this.UserName ?? wikiInfo.UserName ?? throw new InvalidOperationException(Resources.UserNameNotSet);
				var loginPassword = this.Password ?? wikiInfo.Password ?? throw new InvalidOperationException(Resources.PasswordNotSet);
				jobManager.Site.Login(loginName, loginPassword);
				jobManager.Logger = string.IsNullOrEmpty(wikiInfo.LogPage)
					? null
					: new PageJobLogger(jobManager.Site, wikiInfo.LogPage, JobTypes.Write);
				jobManager.ResultHandler = string.IsNullOrEmpty(wikiInfo.ResultsPage)
					? null
					: new PageResultHandler(jobManager.Site, wikiInfo.ResultsPage);
				await jobManager.Run(jobList).ConfigureAwait(true);
			}
			finally
			{
				jobManager.PagePreview -= this.SitePagePreview;
				jobManager.FinishedJob -= this.JobManager_FinishedJob;
				jobManager.StartingJob -= this.JobManager_StartingJob;
			}

			this.Reset();
		}

		private void JobManager_StartingJob(JobManager sender, JobEventArgs eventArgs)
		{
			this.ProgressBarColor = ProgressBarGreen;
			this.jobStarted = DateTime.UtcNow;
			this.StatusWriteLine("Starting " + eventArgs.Job.Name);
		}

		private void JobManager_FinishedJob(JobManager sender, JobEventArgs eventArgs)
		{
			if (eventArgs.Cancelled)
			{
				MessageBox.Show(Resources.JobCancelled, nameof(HoodBot), MessageBoxButton.OK, MessageBoxImage.Information);
			}
			else if (eventArgs.Exception is Exception e)
			{
				MessageBox.Show(e.GetType().Name + ": " + e.Message, e.Source, MessageBoxButton.OK, MessageBoxImage.Error);
				Debug.WriteLine(e.StackTrace);
			}
			else
			{
				this.completedJobs++;
			}
		}

		private void JobTree_OnSelectionChanged(TreeNode sender, SelectedItemChangedEventArgs e)
		{
			this.parameterFetcher?.ClearParameters();
			this.parameterFetcher = null;
			var enabled = false;
			if (e.Node is JobNode job)
			{
				var parameters = job.JobInfo.Parameters;
				if (e.Selected && parameters.Count > 0)
				{
					this.parameterFetcher = new MainWindowParameterFetcher(job.JobInfo);
					this.parameterFetcher.GetParameters();
					enabled = job.IsChecked == true;
				}
			}

			this.JobParameterVisibility = this.parameterFetcher != null ? Visibility.Visible : Visibility.Hidden;
			this.JobParametersEnabled = enabled;
		}

		private void OpenEditWindow()
		{
			// For full MVVM compliance, this should actually be opening the window from an IWindowFactory-type class, but for now, this is fine.
			new SettingsWindow().Show();
			this.MessengerInstance.Send<MainViewModel, SettingsViewModel>(this);
		}

		private void PauseJobs()
		{
			if (this.pauser != null)
			{
				this.PauseJobs(!this.pauser.IsPaused);
			}
		}

		private void PauseJobs(bool isPaused)
		{
			if (this.pauser != null)
			{
				this.pauser.IsPaused = isPaused;
				this.ProgressBarColor = isPaused ? ProgressBarYellow : ProgressBarGreen;
			}
		}

		private void ProgressChanged(double e)
		{
			this.OverallProgress = this.completedJobs + e;
			var timeDiff = DateTime.UtcNow - this.jobStarted;
			if (this.OverallProgress > 0 && timeDiff.TotalSeconds > 0)
			{
				try
				{
					this.UtcEta = this.jobStarted + TimeSpan.FromTicks((long)(timeDiff.Ticks * this.OverallProgressMax / this.OverallProgress));
				}
				catch (ArgumentOutOfRangeException)
				{
					this.UtcEta = DateTime.UtcNow;
				}
			}

			App.WpfYield();
		}

		private void Reset()
		{
			App.WpfYield();
			this.OverallProgress = 0;
			this.OverallProgressMax = 1;
			this.UtcEta = null;

			this.completedJobs = 0;
			this.jobStarted = DateTime.MinValue;
		}

		private void RunTest()
		{
			// Dummy code just so this doesn't get all kinds of unwanted code suggestions.
			Debug.WriteLine(this.selectedItem?.DisplayName);
			Debug.WriteLine(this.UserName);
		}

		private void SitePagePreview(JobManager sender, DiffContent eventArgs)
		{
			// Until we get a menu going, specify manually.
			// currentViewer ??= this.FindPlugin<IDiffViewer>("IeDiff");
			if (this.diffViewer != null && this.ShowDiffs)
			{
				this.diffViewer.Compare(eventArgs);
				this.diffViewer.Wait();
			}
		}

		private void StatusWrite(string text)
		{
			this.Status += (this.Status?.Length ?? 0) == 0 ? text.TrimStart() : text;
			App.WpfYield();
		}

		private void StatusWriteLine(string text) => this.StatusWrite(text + NewLine);
		#endregion
	}
}