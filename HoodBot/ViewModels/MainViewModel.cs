namespace RobinHood70.HoodBot.ViewModels;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs;
using RobinHood70.HoodBot.Jobs.Design;
using RobinHood70.HoodBot.Models;
using RobinHood70.HoodBot.Properties;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.HoodBot.Views;
using RobinHood70.HoodBotPlugins;
using RobinHood70.Robby;

public class MainViewModel : ObservableRecipient
{
	#region Static Fields
	private static readonly Brush ProgressBarGreen = new SolidColorBrush(Color.FromArgb(255, 6, 176, 37));
	private static readonly Brush ProgressBarYellow = new SolidColorBrush(Color.FromArgb(255, 255, 240, 0));
	#endregion

	#region Fields
	private readonly IDiffViewer? diffViewer;
	private readonly PauseTokenSource pauser = new();

	private CancellationTokenSource? canceller;
	private DateTime? eta;
	private bool executing;
	private DateTime timerStarted;
	private MainWindowParameterFetcher? parameterFetcher; // was IParameterFetcher?
	#endregion

	#region Constructors
	public MainViewModel()
	{
		// This probably shouldn't be here but rather in some kind of initialization for all sites. For now, however, this is quick and dirty.
		Site.RegisterSiteClass(UespSite.CreateInstance, nameof(UespSite));

		this.SelectedItem = App.UserSettings.GetCurrentItem();
		var plugins = Plugins.Instance;
		this.diffViewer = plugins.DiffViewers["Internet Explorer"];
		this.JobParameterVisibility = Visibility.Hidden;
		this.ResetProgress();
		this.ProgressBarColor = ProgressBarGreen;
		this.Status = string.Empty;

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
				if (!this.executing && this.SelectedItem is WikiInfoViewModel wikiInfo)
				{
					this.executing = true;
					this.StatusWriteLine("Initializing");
					await this.ExecuteJobs(wikiInfo).ConfigureAwait(true);
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
		get;
		set => this.SetProperty(ref field, value);
	}

	public DateTime? Eta => this.eta?.ToLocalTime();

	public bool JobParametersEnabled
	{
		get;
		private set => this.SetProperty(ref field, value);
	}

	public Visibility JobParameterVisibility
	{
		get;
		private set => this.SetProperty(ref field, value);
	}

	public TreeNode JobTree { get; } = JobNode.Populate();

	public double Progress
	{
		get;
		private set => this.SetProperty(ref field, value);
	}

	public double ProgressMax
	{
		get;
		private set => this.SetProperty(ref field, value < 1 ? 1 : value);
	}

	public string? Password
	{
		get;
		set => this.SetProperty(ref field, value);
	}

	public Brush ProgressBarColor
	{
		get;
		set => this.SetProperty(ref field, value);
	}

	public WikiInfoViewModel? SelectedItem
	{
		get;
		set
		{
			if (value != null)
			{
				var userSettings = App.UserSettings;
				if (!userSettings.SelectedName.OrdinalEquals(value.DisplayName))
				{
					userSettings.SelectedName = value.DisplayName;
				}
			}

			this.SetProperty(ref field, value);
		}
	}

	public string Status
	{
		get;
		set => this.SetProperty(ref field, value ?? string.Empty);
	}

	public UserSettings UserSettings { get; } = App.UserSettings;

	public string? UserName
	{
		get;
		set => this.SetProperty(ref field, value);
	}

	public DateTime? UtcEta
	{
		get => this.eta;
		private set
		{
			if (this.SetProperty(ref this.eta, value))
			{
				this.OnPropertyChanged();
				this.OnPropertyChanged(nameof(this.eta));
			}
		}
	}
	#endregion

	#region Private Static Methods
	private static string FormatTimeSpan(TimeSpan allJobsTimer)
	{
		var retval = allJobsTimer.ToString(@"h\h\ m\m\ s\.f\s", CultureInfo.CurrentCulture);
		if (retval.StartsWith("0h ", StringComparison.Ordinal))
		{
			retval = retval[3..];
			if (retval.StartsWith("0m ", StringComparison.Ordinal))
			{
				retval = retval[3..];
			}
		}

		if (retval.EndsWith(".0s", StringComparison.Ordinal))
		{
			retval = retval.Remove(retval.Length - 3, 2);
		}

		if (retval.EndsWith(" 0s", StringComparison.Ordinal))
		{
			retval = retval[..^3];
		}

		if (retval.EndsWith(" 0m", StringComparison.Ordinal))
		{
			retval = retval[..^3];
		}

		return retval
			.Replace(".0", string.Empty, StringComparison.Ordinal)
			.Replace(" 0s", string.Empty, StringComparison.Ordinal);
	}
	#endregion

	#region Private Methods
	private void CancelJobs()
	{
		this.canceller?.Cancel();
		this.ResetProgress();
		this.PauseJobs(isPaused: false);
	}

	private void ClearStatus() => this.StatusWrite(null);

	private async Task ExecuteJobs(WikiInfoViewModel wikiInfo)
	{
		this.parameterFetcher?.SetParameters();

		List<JobInfo> jobList = [];
		var login = false;
		foreach (var node in this.JobTree.CheckedChildren<JobNode>())
		{
			jobList.Add(node.JobInfo);
			login |= node.JobInfo.Login;
		}

		if (jobList.Count == 0)
		{
			this.executing = false;
			return;
		}

		this.ClearStatus();

		// This is re-initialized every time so that one cancellation doesn't auto-cancel anything you start after it. I don't believe this is necessary for pausing, though.
		this.canceller = new();

		using var jobManager = new JobManager(wikiInfo.WikiInfo, this.EditingEnabled, this.pauser, this.canceller);
		try
		{
			jobManager.StartingJob += this.JobManager_StartingJob;
			jobManager.FinishedJob += this.JobManager_FinishedJob;
			jobManager.ProgressReset += this.JobManager_ProgressReset;
			jobManager.ProgressUpdated += this.JobManager_ProgressUpdated;
			jobManager.StatusUpdated += this.JobManager_StatusUpdated;
			jobManager.PagePreview += this.SitePagePreview;

			if (login)
			{
				jobManager.Login(
					this.UserName ?? wikiInfo.UserName ?? throw new InvalidOperationException(Resources.UserNameNotSet),
					this.Password ?? wikiInfo.Password ?? throw new InvalidOperationException(Resources.PasswordNotSet),
					wikiInfo.LogPage,
					wikiInfo.ResultsPage);
			}

			var allJobsTimer = Stopwatch.GetTimestamp();
			await jobManager.Run(jobList).ConfigureAwait(true);
			this.StatusWriteLine($"Total time for last run: {FormatTimeSpan(Stopwatch.GetElapsedTime(allJobsTimer))}");
		}
		finally
		{
			jobManager.PagePreview -= this.SitePagePreview;
			jobManager.StatusUpdated -= this.JobManager_StatusUpdated;
			jobManager.ProgressUpdated -= this.JobManager_ProgressUpdated;
			jobManager.ProgressReset -= this.JobManager_ProgressReset;
			jobManager.FinishedJob -= this.JobManager_FinishedJob;
			jobManager.StartingJob -= this.JobManager_StartingJob;
		}

		this.ResetProgress();
	}

	private void JobManager_StartingJob(JobManager sender, JobEventArgs eventArgs)
	{
		this.ProgressBarColor = ProgressBarGreen;
		this.ResetProgress();
		this.StatusWriteLine($"Starting {eventArgs.Job.Name}");
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
		this.Messenger.Send(this, nameof(SettingsViewModel));
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

	private void JobManager_ProgressReset(JobManager sender, EventArgs e) => this.ResetProgress();

	private void JobManager_ProgressUpdated(JobManager sender, double e) => this.UpdateProgress(e);

	private void JobManager_StatusUpdated(JobManager sender, string? text) => this.StatusWrite(text);

	private void ResetProgress()
	{
		this.Progress = 0;
		this.ProgressMax = 1;
		this.UtcEta = null;
		this.timerStarted = DateTime.UtcNow;
	}

	private void RunTest()
	{
		// Dummy code just so this doesn't get all kinds of unwanted code suggestions.
		Debug.WriteLine(this.SelectedItem?.DisplayName);
		Debug.WriteLine(this.UserName);
	}

	private void SitePagePreview(JobManager sender, DiffContent eventArgs)
	{
		// Until we get a menu going, specify manually.
		// currentViewer ??= this.FindPlugin<IDiffViewer>("IeDiff");
		if (this.diffViewer != null && sender.ShowDiffs)
		{
			this.diffViewer.Compare(eventArgs);
			this.diffViewer.Wait();
		}
	}

	private void StatusWrite(string? text)
	{
		if (text is null)
		{
			// null can be passed both internally and StatusUpdate event to clear status box.
			this.Status = string.Empty;
		}
		else if (this.Status.Length == 0)
		{
			text = text.TrimStart(TextArrays.NewLineChars);
			if (text.Length > 0)
			{
				this.Status = text;
			}
		}
		else if (text.Length > 0)
		{
			this.Status += text;
		}
	}

	private void StatusWriteLine(string text) => this.StatusWrite(text + Environment.NewLine);

	private void UpdateProgress(double progress)
	{
		this.Progress = progress;
		var timeDiff = DateTime.UtcNow - this.timerStarted;
		if (this.Progress > 0 && timeDiff.TotalSeconds > 0)
		{
			try
			{
				this.UtcEta = this.timerStarted + TimeSpan.FromTicks((long)(timeDiff.Ticks * this.ProgressMax / this.Progress));
			}
			catch (ArgumentOutOfRangeException)
			{
				this.UtcEta = DateTime.UtcNow;
			}
		}
	}
	#endregion
}