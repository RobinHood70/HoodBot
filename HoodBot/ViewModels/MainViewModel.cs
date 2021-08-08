namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Media;
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.Command;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot;
	using RobinHood70.HoodBot.Jobs;
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

	public class MainViewModel : ViewModelBase
	{
		#region Private Constants
		private const string Books29Path = @"D:\Books29\";
		private const string Books30Path = @"D:\Books30\";
		#endregion

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

			this.progressMonitor = new Progress<double>(this.ProgressChanged);
			this.statusMonitor = new Progress<string>(this.StatusWrite);

			Site.RegisterSiteClass(Uesp.UespSite.CreateInstance, "UespHoodBot");

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
				async void ExecuteJobsAsync() => await this.ExecuteJobs().ConfigureAwait(true);
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

		#region Internal Static Methods
#if DEBUG
		internal static string SiteName(IWikiAbstractionLayer sender) => sender.AllSiteInfo?.General?.SiteName ?? "Site-Agnostic";
#endif
		#endregion

		#region Protected Methods
#if DEBUG
		// These are flagged as internal mostly to stop warnings whenever they're not in use.
		protected virtual void SiteChanging(Site sender, ChangeArgs eventArgs)
		{
			Debug.Write($"{eventArgs.MethodName} (sender: {eventArgs.RealSender}");
			foreach (var parameter in eventArgs.Parameters)
			{
				Debug.Write($", {parameter.Key}: {parameter.Value}");
			}

			Debug.WriteLine(")");
		}

		protected virtual void SiteWarningOccurred(Site sender, WarningEventArgs eventArgs) => Debug.WriteLine(eventArgs?.Warning);

		protected virtual void WalResponseRecieved(IWikiAbstractionLayer sender, ResponseEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Response: {eventArgs.Response}");

		protected virtual void WalSendingRequest(IWikiAbstractionLayer sender, RequestEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Request: {eventArgs.Request}");

		protected virtual void WalWarningOccurred(IWikiAbstractionLayer sender, WallE.Design.WarningEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Warning: ({eventArgs?.Warning.Code}) {eventArgs?.Warning.Info}");
#endif
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
			this.canceller?.Cancel();
			this.Reset();
			this.PauseJobs(isPaused: false);
		}

		private void ClearStatus() => this.Status = string.Empty; // TODO: Removed from Reset, so add to a button or maybe only on Play.

		private void Client_RequestingDelay(IMediaWikiClient sender, DelayEventArgs eventArgs)
		{
			this.StatusWriteLine(Globals.CurrentCulture(Resources.DelayRequested, eventArgs.Reason, $"{eventArgs.DelayTime.TotalSeconds.ToString(CultureInfo.CurrentCulture)}s", eventArgs.Description));
			App.WpfYield();

			/*
			// Half-assed workaround for pausing and cancelling that ultimately just ends in the wiki throwing an error. See TODO in SimpleClient.RequestDelay().
			if (this.pauser.IsPaused || this.canceller.IsCancellationRequested)
			{
				eventArgs.Cancel = true;
			}
			*/
		}

		private IWikiAbstractionLayer CreateAbstractionLayer(IMediaWikiClient client, WikiInfoViewModel wikiInfo)
		{
			var api = wikiInfo.Api.NotNull(nameof(wikiInfo), nameof(wikiInfo.Api));
			IWikiAbstractionLayer abstractionLayer = string.Equals(api.OriginalString, "/", StringComparison.Ordinal)
				? new WallE.Test.WikiAbstractionLayer()
				: new WikiAbstractionLayer(client, api);
			if (abstractionLayer is IMaxLaggable maxLagWal)
			{
				maxLagWal.MaxLag = wikiInfo.MaxLag;
			}

#if DEBUG
			if (abstractionLayer is IInternetEntryPoint internet)
			{
				internet.SendingRequest += this.WalSendingRequest;
				//// internet.ResponseReceived += WalResponseRecieved;
			}

			abstractionLayer.WarningOccurred += this.WalWarningOccurred;
#endif

			return abstractionLayer;
		}

		private IMediaWikiClient CreateClient(WikiInfoViewModel wikiInfo, CancellationToken cancellationToken)
		{
			IMediaWikiClient client = new SimpleClient(App.UserSettings.ContactInfo, Path.Combine(App.UserFolder, "Cookies.json"), cancellationToken);
			if (wikiInfo.ReadThrottling > 0 || wikiInfo.WriteThrottling > 0)
			{
				client = new ThrottledClient(
					client,
					TimeSpan.FromMilliseconds(wikiInfo.ReadThrottling),
					TimeSpan.FromMilliseconds(wikiInfo.WriteThrottling));
			}

			client.RequestingDelay += this.Client_RequestingDelay;

			return client;
		}

		private JobManager CreateJobManager(Site site, WikiInfoViewModel wikiInfo, CancellationToken cancellationToken)
		{
			// We pass wikiInfo here only because it's already validated as not null.
			PauseTokenSource pauseSource = new();
			this.pauser = pauseSource;
			var pauseToken = pauseSource.Token;

			JobManager jobManager = new(site, pauseToken, cancellationToken)
			{
				Logger = string.IsNullOrEmpty(wikiInfo.LogPage)
					? null
					: new PageJobLogger(site, wikiInfo.LogPage, JobTypes.Write),
				ProgressMonitor = this.progressMonitor,
				ResultHandler = string.IsNullOrEmpty(wikiInfo.ResultsPage)
					? null
					: new PageResultHandler(site, wikiInfo.ResultsPage),
				StatusMonitor = this.statusMonitor,
			};

			jobManager.StartingJob += this.JobManager_StartingJob;
			jobManager.FinishedJob += this.JobManager_FinishedJob;
			return jobManager;
		}

		private void DestroyJobManager(JobManager jobManager)
		{
			jobManager.FinishedJob -= this.JobManager_FinishedJob;
			jobManager.StartingJob -= this.JobManager_StartingJob;
			this.pauser = null;
		}

		private Site CreateSite(IWikiAbstractionLayer abstractionLayer, WikiInfoViewModel wikiInfo)
		{
			var factoryMethod = Site.GetFactoryMethod(wikiInfo.SiteClassIdentifier);
			var site = factoryMethod(abstractionLayer);
#if DEBUG
			site.WarningOccurred += this.SiteWarningOccurred;
			site.Changing += this.SiteChanging;
#endif
			site.PagePreview += this.SitePagePreview;
			site.EditingEnabled = this.EditingEnabled;
			if ((this.UserName ?? wikiInfo.UserName) is string user)
			{
				var currentPassword = this.Password ?? wikiInfo.Password ?? throw new InvalidOperationException(Resources.PasswordNotSet);
				site.Login(user, currentPassword);
			}

			return site;
		}

		private void DestroyAbstractionLayer(IWikiAbstractionLayer abstractionLayer)
		{
#if DEBUG
			abstractionLayer.WarningOccurred -= this.WalWarningOccurred;
			if (abstractionLayer is IInternetEntryPoint internet)
			{
				internet.SendingRequest -= this.WalSendingRequest;
			}
#endif
		}

		private void DestroyClient(IMediaWikiClient client) => client.RequestingDelay -= this.Client_RequestingDelay;

		private void DestroySite(Site site)
		{
			site.PagePreview -= this.SitePagePreview;
#if DEBUG
			site.Changing -= this.SiteChanging;
			site.WarningOccurred -= this.SiteWarningOccurred;
#endif
		}

		private async Task ExecuteJobs()
		{
			if (this.executing || this.selectedItem is not WikiInfoViewModel wikiInfo)
			{
				return;
			}

			this.StatusWriteLine("Initializing");
			App.WpfYield();

			this.executing = true;
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

			Stopwatch allJobsTimer = new();
			allJobsTimer.Start();
			this.ClearStatus();
			this.completedJobs = 0;
			this.OverallProgressMax = jobList.Count;

			using CancellationTokenSource cancelSource = new();
			this.canceller = cancelSource;
			var cancellationToken = cancelSource.Token;

			var client = this.CreateClient(wikiInfo, cancellationToken);
			var abstractionLayer = this.CreateAbstractionLayer(client, wikiInfo);
			var site = this.CreateSite(abstractionLayer, wikiInfo);

			var jobManager = this.CreateJobManager(site, wikiInfo, cancellationToken);
			await jobManager.Run(jobList).ConfigureAwait(true);

			this.DestroyJobManager(jobManager);
			this.DestroySite(site);
			this.DestroyAbstractionLayer(abstractionLayer);
			this.DestroyClient(client);
			this.canceller = null;

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

		private void RunTest()
		{
			// Compare Books non-job for Jeancey.
			List<string> deleted = new();
			List<string> added = new();
			List<string> common = new();

			var fullNames29 = Directory.GetFiles(Books29Path);
			var fullNames30 = Directory.GetFiles(Books30Path);
			HashSet<string> dir29 = new(StringComparer.OrdinalIgnoreCase);
			HashSet<string> dir30 = new(StringComparer.OrdinalIgnoreCase);
			foreach (var book in fullNames29)
			{
				dir29.Add(Path.GetFileName(book));
			}

			foreach (var book in fullNames30)
			{
				dir30.Add(Path.GetFileName(book));
			}

			foreach (var book in dir29)
			{
				if (dir30.Contains(book))
				{
					common.Add(book);
				}
				else
				{
					deleted.Add(book);
				}
			}

			foreach (var book in dir30)
			{
				if (!dir29.Contains(book))
				{
					added.Add(book);
				}
			}

			common.Sort(StringComparer.OrdinalIgnoreCase);
			foreach (var book in common)
			{
				var book29 = File.ReadAllText(Books29Path + book);
				var book30 = File.ReadAllText(Books30Path + book);
				book29 = Regex.Replace(book29, @"\s+", " ", RegexOptions.None, Globals.DefaultRegexTimeout);
				book30 = Regex.Replace(book30, @"\s+", " ", RegexOptions.None, Globals.DefaultRegexTimeout);

				if (!string.Equals(book29, book30, StringComparison.Ordinal))
				{
					Debug.WriteLine(book + " has changed");
				}
			}

			deleted.Sort(StringComparer.OrdinalIgnoreCase);
			foreach (var book in deleted)
			{
				Debug.WriteLine(book + " deleted");
			}

			added.Sort(StringComparer.OrdinalIgnoreCase);
			foreach (var book in added)
			{
				Debug.WriteLine(book + " is new");
			}

			// Dummy code just so this doesn't get all kinds of unwanted code suggestions.
			Debug.WriteLine(this.selectedItem?.DisplayName);
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
				DiffContent diffContent = new(page.FullPageName, page.Text ?? string.Empty, eventArgs.EditSummary, eventArgs.Minor)
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