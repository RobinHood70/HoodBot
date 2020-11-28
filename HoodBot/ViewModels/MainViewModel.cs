namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Threading;
	using System.Windows;
	using System.Windows.Media;
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.Command;
	using RobinHood70.HoodBot;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Loggers;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.HoodBot.Properties;
	using RobinHood70.HoodBot.Views;
	using RobinHood70.HoodBotPlugins;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Eve;
	using static System.Environment;
	using static RobinHood70.CommonCode.Globals;

	// FIXME: Fix re-runs with different parameters (i.e., a different template for template usage) uses existing parameters instead of new ones.
	public class MainViewModel : ViewModelBase
	{
		#region Static Fields
		private static readonly Brush ProgressBarGreen = new SolidColorBrush(Color.FromArgb(255, 6, 176, 37));
		private static readonly Brush ProgressBarYellow = new SolidColorBrush(Color.FromArgb(255, 255, 240, 0));
		#endregion

		#region Fields
		private readonly IDiffViewer? diffViewer;
		private readonly IProgress<double> progressMonitor;
		private readonly IProgress<string> statusMonitor;

		private CancellationTokenSource? canceller;
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
		private PauseTokenSource? pauser;
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

			this.Client = new SimpleClient(App.UserSettings.ContactInfo, Path.Combine(App.UserFolder, "Cookies.json"));
			this.Client.RequestingDelay += this.Client_RequestingDelay;

			this.progressMonitor = new Progress<double>(this.ProgressChanged);
			this.statusMonitor = new Progress<string>(this.StatusWrite);

			Site.RegisterSiteClass(Uesp.UespSite.CreateInstance, "UespHoodBot");

			var plugins = Plugins.Instance;
			this.diffViewer = plugins.DiffViewers["Internet Explorer"];

			this.JobTree.SelectionChanged += this.JobTree_OnSelectionChanged;
		}
		#endregion

		#region Public Commands
		public RelayCommand EditSettings => new RelayCommand(this.OpenEditWindow);

		public RelayCommand Play => new RelayCommand(this.ExecuteJobs);

		public RelayCommand Pause => new RelayCommand(this.PauseJobs);

		public RelayCommand Stop => new RelayCommand(this.CancelJobs);

		public RelayCommand Test => new RelayCommand(this.RunTest);
		#endregion

		#region Public Properties
		public IMediaWikiClient Client { get; }

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

		public bool ShowDiffs { get; private set; } = true; // Simple hard-coded setting for now.

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

		private static IWikiAbstractionLayer GetAbstractionLayer(IMediaWikiClient client, WikiInfoViewModel info)
		{
			ThrowNull(info.Api, nameof(info), nameof(info.Api));
			var wal = string.Equals(info.Api.OriginalString, "/", StringComparison.Ordinal)
				? new WallE.Test.WikiAbstractionLayer()
				: (IWikiAbstractionLayer)new WikiAbstractionLayer(client, info.Api);
			if (wal is IMaxLaggable maxLagWal)
			{
				maxLagWal.MaxLag = info.MaxLag;
			}

			return wal;
		}

#if DEBUG
		private static void Site_Changing(Site sender, ChangeArgs eventArgs)
		{
			Debug.WriteLine(eventArgs.MethodName);
			foreach (var parameter in eventArgs.Parameters)
			{
				Debug.WriteLine($"  {parameter.Key} = {parameter.Value}");
			}
		}

		private static string SiteName(IWikiAbstractionLayer sender) => sender.AllSiteInfo?.General?.SiteName ?? "Site-Agnostic";

		private static void SiteWarningOccurred(Site sender, WarningEventArgs eventArgs) => Debug.WriteLine(eventArgs?.Warning);

		private static void WalResponseRecieved(IWikiAbstractionLayer sender, ResponseEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Response: {eventArgs.Response}");

		private static void WalSendingRequest(IWikiAbstractionLayer sender, RequestEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Request: {eventArgs.Request}");

		private static void WalWarningOccurred(IWikiAbstractionLayer sender, WallE.Design.WarningEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Warning: ({eventArgs?.Warning.Code}) {eventArgs?.Warning.Info}");
#endif
		#endregion

		#region Private Methods
		private void CancelJobs()
		{
			this.canceller?.Cancel();
			this.Reset();
			this.PauseJobs(isPaused: false);
		}

		private void ClearStatus() => this.Status = string.Empty; // TODO: Removed from Reset, so add to a button or maybe only on Play.

		private void Client_RequestingDelay(IMediaWikiClient sender, DelayEventArgs eventArgs)
		{
			this.StatusWriteLine(CurrentCulture(Resources.DelayRequested, eventArgs.Reason, $"{eventArgs.DelayTime.TotalSeconds.ToString(CultureInfo.CurrentCulture)}s", eventArgs.Description));
			App.WpfYield();

			/*
			// Half-assed workaround for pausing and cancelling that ultimately just ends in the wiki throwing an error. See TODO in SimpleClient.RequestDelay().
			if (this.pauser.IsPaused || this.canceller.IsCancellationRequested)
			{
				eventArgs.Cancel = true;
			}
			*/
		}

		private async void ExecuteJobs()
		{
			if (this.executing || this.SelectedItem == null)
			{
				return;
			}

			this.executing = true;
			this.parameterFetcher?.SetParameters();

			var jobList = new List<JobInfo>();
			foreach (var node in this.JobTree.CheckedChildren<JobNode>())
			{
				jobList.Add(node.JobInfo);
			}

			if (jobList.Count == 0)
			{
				this.executing = false;
				return;
			}

			var allJobsTimer = new Stopwatch();
			allJobsTimer.Start();
			this.ClearStatus();
			this.completedJobs = 0;
			this.OverallProgressMax = jobList.Count;
			using (var cancelSource = new CancellationTokenSource())
			{
				this.canceller = cancelSource;
				this.pauser = new PauseTokenSource();

				this.StatusWriteLine("Initializing");
				App.WpfYield();
				var site = this.InitializeSite();
				var jobManager = new JobManager(site)
				{
					CancellationToken = this.canceller?.Token,
					PauseToken = this.pauser?.Token,
					ProgressMonitor = this.progressMonitor,
					StatusMonitor = this.statusMonitor,
				};

				if (!string.IsNullOrEmpty(this.SelectedItem.LogPage))
				{
					jobManager.Logger = new PageJobLogger(site, this.SelectedItem.LogPage, JobTypes.Write);
				}

				if (!string.IsNullOrEmpty(this.SelectedItem.ResultsPage))
				{
					jobManager.ResultHandler = new PageResultHandler(site, this.SelectedItem.ResultsPage);
				}

				jobManager.StartingJob += this.JobManager_StartingJob;
				jobManager.FinishedJob += this.JobManager_FinishedJob;
				await jobManager.Run(jobList).ConfigureAwait(false);

				this.ResetSite(site);
				this.pauser = null;
				this.canceller = null;
			}

			this.Reset();
			this.StatusWriteLine("Total time for last run: " + FormatTimeSpan(allJobsTimer.Elapsed));
			this.executing = false;
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

		private Site InitializeSite()
		{
			var wikiInfo = this.SelectedItem ?? throw new InvalidOperationException(Resources.NoWiki);
			var abstractionLayer = GetAbstractionLayer(this.Client, wikiInfo);

#if DEBUG
			if (abstractionLayer is IInternetEntryPoint internet)
			{
				internet.SendingRequest += WalSendingRequest;
				//// internet.ResponseReceived += WalResponseRecieved;
			}

			abstractionLayer.WarningOccurred += WalWarningOccurred;
#endif
			var factoryMethod = Site.GetFactoryMethod(wikiInfo.SiteClassIdentifier);
			var site = factoryMethod(abstractionLayer);
#if DEBUG
			site.WarningOccurred += SiteWarningOccurred;
#endif
			site.PagePreview += this.SitePagePreview;
			site.EditingEnabled = this.EditingEnabled;
			if ((this.UserName ?? wikiInfo.UserName) is string user)
			{
				var currentPassword = this.Password ?? wikiInfo.Password ?? throw new InvalidOperationException(Resources.PasswordNotSet);
				site.Login(user, currentPassword);
			}

			site.Changing += Site_Changing;

			return site;
		}

		private void JobTree_OnSelectionChanged(TreeNode sender, SelectedItemChangedEventArgs e)
		{
			this.parameterFetcher?.SetParameters();
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
				this.UtcEta = this.jobStarted + TimeSpan.FromTicks((long)(timeDiff.Ticks * this.OverallProgressMax / this.OverallProgress));
			}
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

		private void ResetSite(Site site)
		{
#if DEBUG
			if (site.AbstractionLayer is IInternetEntryPoint internet)
			{
				internet.SendingRequest -= WalSendingRequest;
			}

			site.Changing -= Site_Changing;
			site.AbstractionLayer.WarningOccurred -= WalWarningOccurred;
			site.WarningOccurred -= SiteWarningOccurred;
#endif
			site.PagePreview -= this.SitePagePreview;
		}

		private void RunTest()
		{
			// Dummy code just so this doesn't get all kinds of unwanted code suggestions.
			Debug.WriteLine(this.SelectedItem?.DisplayName);
			Debug.WriteLine(this.UserName);
		}

		private void SitePagePreview(Site sender, PagePreviewArgs eventArgs)
		{
			// Until we get a menu going, specify manually.
			// currentViewer ??= this.FindPlugin<IDiffViewer>("IeDiff");
			if (this.diffViewer != null && this.ShowDiffs && sender.AbstractionLayer is ITokenGenerator tokens)
			{
				var token = tokens.TokenManager.SessionToken("csrf");
				var page = eventArgs.Page;
				var diffContent = new DiffContent(page.FullPageName, page.Text ?? string.Empty, eventArgs.EditSummary, eventArgs.Minor)
				{
					EditPath = page.EditPath,
					EditToken = token,
					LastRevisionText = page.CurrentRevision?.Text,
					LastRevisionTimestamp = page.CurrentRevision?.Timestamp,
					StartTimestamp = page.StartTimestamp,
				};
				this.diffViewer.Compare(diffContent);
				this.diffViewer.Wait();
			}
		}

		private void StatusWrite(string text) => this.Status += (this.Status?.Length ?? 0) == 0 ? text.TrimStart() : text;

		private void StatusWriteLine(string text) => this.StatusWrite(text + NewLine);
		#endregion
	}
}