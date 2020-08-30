namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Media;
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.Command;
	using RobinHood70.HoodBot;
	using RobinHood70.HoodBot.Jobs.Design;
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

	// TODO: Decouple this into a job-runner class, or something along those lines, that notifies this one of updates.
	public class MainViewModel : ViewModelBase
	{
		#region Private Constants
		private const string ContactInfo = "robinhood70@live.ca";
		#endregion

		#region Static Fields
		private static readonly Brush ProgressBarGreen = new SolidColorBrush(Color.FromArgb(255, 6, 176, 37));
		private static readonly Brush ProgressBarYellow = new SolidColorBrush(Color.FromArgb(255, 255, 240, 0));
		#endregion

		#region Fields
		private readonly string appDataFolder;
		private readonly IProgress<double> progressMonitor;
		private readonly IProgress<string> statusMonitor;

		private CancellationTokenSource? canceller;
		private double completedJobs;
		private WikiInfo? currentItem;
		private bool editingEnabled;
		private DateTime? eta;
		private bool executing;
		private bool jobParametersEnabled;
		private Visibility jobParameterVisibility = Visibility.Hidden;
		private DateTime jobStarted;
		private double overallProgress;
		private double overallProgressMax = 1;
		private string? password;
		private PauseTokenSource? pauser;
		private Brush progressBarColor = ProgressBarGreen;
		private string status = string.Empty;
		private string? userName;
		#endregion

		#region Constructors
		public MainViewModel()
		{
			// ThrowNull(options, nameof(options));
			// this.settings = options.Value;
			this.appDataFolder = Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.Create), nameof(HoodBot));
			this.Client = new SimpleClient(ContactInfo, Path.Combine(this.appDataFolder, "Cookies.dat"));
			this.Client.RequestingDelay += this.Client_RequestingDelay;
			this.UserSettings = UserSettings.Load(Path.Combine(this.appDataFolder, "Settings.json"));
			this.SelectedItem = this.UserSettings.GetCurrentItem();
			this.progressMonitor = new Progress<double>(this.ProgressChanged);
			this.statusMonitor = new Progress<string>(this.StatusWrite);
			Site.RegisterSiteClass(Uesp.UespSite.CreateInstance, "UespHoodBot");
			var plugins = Plugins.Instance;
			this.DiffViewer = plugins.DiffViewers["Internet Explorer"];
			var jobs = JobNode.Populate();
			this.JobTree.SelectionChanged += this.JobTree_OnSelectionChanged;
		}
		#endregion

		#region Destructor
		~MainViewModel()
		{
			this.UserSettings.Save();
			this.Client.RequestingDelay -= this.Client_RequestingDelay;
		}
		#endregion

		#region Public Properties
		public IMediaWikiClient Client { get; }

		public IDiffViewer? DiffViewer { get; set; }

		public bool EditingEnabled
		{
			get => this.editingEnabled;
			set => this.Set(ref this.editingEnabled, value, nameof(this.EditingEnabled));
		}

		public RelayCommand EditSettings => new RelayCommand(this.OpenEditWindow);

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

		public RelayCommand Play => new RelayCommand(this.ExecuteJobs);

		public RelayCommand Pause => new RelayCommand(this.PauseJobs);

		public Brush ProgressBarColor
		{
			get => this.progressBarColor;
			set => this.Set(ref this.progressBarColor, value, nameof(this.ProgressBarColor));
		}

		public WikiInfo? SelectedItem
		{
			get => this.currentItem;
			set
			{
				if (value != null)
				{
					this.UserSettings.UpdateCurrentWiki(value);
					this.UserSettings.Save();
				}

				this.Set(ref this.currentItem, value, nameof(this.SelectedItem));
			}
		}

		public bool ShowDiffs { get; private set; } = true; // Simple hard-coded setting for now.

		public string Status
		{
			get => this.status;
			set => this.Set(ref this.status, value ?? string.Empty, nameof(this.Status));
		}

		public RelayCommand Stop => new RelayCommand(this.CancelJobs);

		public RelayCommand Test => new RelayCommand(this.RunTest);

		public string? UserName
		{
			get => this.userName;
			set => this.Set(ref this.userName, value, nameof(this.UserName));
		}

		public UserSettings UserSettings { get; }

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

		#region Internal Properties
		internal IParameterFetcher? ParameterFetcher { get; set; } = new MainWindowParameterFetcher();
		#endregion

		#region Internal Static Methods

#if DEBUG
		internal static void SiteWarningOccurred(Site sender, WarningEventArgs eventArgs) => Debug.WriteLine(eventArgs?.Warning);

		internal static void WalResponseRecieved(IWikiAbstractionLayer sender, ResponseEventArgs eventArgs) => Debug.WriteLine($"{sender.SiteName} Response: {eventArgs.Response}");

		internal static void WalSendingRequest(IWikiAbstractionLayer sender, RequestEventArgs eventArgs) => Debug.WriteLine($"{sender.SiteName} Request: {eventArgs.Request}");

		internal static void WalWarningOccurred(IWikiAbstractionLayer sender, WallE.Design.WarningEventArgs eventArgs) => Debug.WriteLine($"{sender.SiteName} Warning: ({eventArgs?.Warning.Code}) {eventArgs?.Warning.Info}");
#endif
		#endregion

		#region Internal Methods
		internal void GetParametersFor(JobNode jobNode)
		{
			if (this.ParameterFetcher != null && jobNode?.JobInfo != null)
			{
				foreach (var param in jobNode.JobInfo.Parameters)
				{
					this.ParameterFetcher.GetParameter(param);
				}
			}
		}

		internal void SetParametersOn(JobNode jobNode)
		{
			if (this.ParameterFetcher != null && jobNode?.JobInfo != null)
			{
				foreach (var param in jobNode.JobInfo.Parameters)
				{
					this.ParameterFetcher.SetParameter(param);
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

		private static IWikiAbstractionLayer GetAbstractionLayer(IMediaWikiClient client, WikiInfo info)
		{
			var wal = info.Api == null
			? throw PropertyNull(nameof(WikiInfo), nameof(info.Api))
			: new WikiAbstractionLayer(client, info.Api);
			if (wal is IMaxLaggable maxLagWal)
			{
				maxLagWal.MaxLag = info.MaxLag;
			}

			return wal;
		}

		private void OpenEditWindow()
		{
			// For full MVVM compliance, this should actually be opening the window from an IWindowFactory-type class, but for now, this is fine.
			new SettingsWindow().Show();
			this.MessengerInstance.Send<MainViewModel, SettingsViewModel>(this);
		}
		#endregion

		#region Private Methods
		private void CancelJobs()
		{
			this.canceller?.Cancel();
			this.Reset();
			this.PauseJobs(false);
		}

		private void ClearStatus() => this.Status = string.Empty; // TODO: Removed from Reset, so add to a button or maybe only on Play.

		private void Client_RequestingDelay(IMediaWikiClient sender, DelayEventArgs eventArgs)
		{
			this.StatusWriteLine(CurrentCulture(Resources.DelayRequested, eventArgs.Reason, eventArgs.DelayTime.TotalSeconds + "s", eventArgs.Description));
			App.WpfYield();

			/*
			// Half-assed workaround for pausing and cancelling that ultimately just ends in the wiki throwing an error. See TODO in SimpleClient.RequestDelay().
			if (this.pauser.IsPaused || this.canceller.IsCancellationRequested)
			{
				eventArgs.Cancel = true;
			}
			*/
		}

		private WikiJob ConstructJob(JobNode selectedNode, Site site)
		{
			ThrowNull(selectedNode, nameof(selectedNode));
			var jobNode = selectedNode.JobInfo;
			var objectList = new List<object?> { site, new AsyncInfo(this.progressMonitor, this.statusMonitor, this.pauser?.Token, this.canceller?.Token) };

			if (jobNode.Parameters is IReadOnlyList<ConstructorParameter> jobParams)
			{
				foreach (var param in jobParams)
				{
					objectList.Add(param.Attribute is JobParameterFileAttribute && param.Value is string value ? ExpandEnvironmentVariables(value) : param.Value);
				}
			}

			return (WikiJob)jobNode.Constructor.Invoke(objectList.ToArray());
		}

		private async void ExecuteJobs()
		{
			if (this.executing)
			{
				return;
			}

			this.executing = true;
			if (this.JobTree.SelectedItem is JobNode selectedNode)
			{
				this.SetParametersOn(selectedNode);
			}

			var jobList = new List<JobNode>(this.JobTree.CheckedChildren<JobNode>());
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
				var success = true;
				var site = this.InitializeSite();
				var jobRunner = site as IJobAware;
				jobRunner?.OnJobsStarted();

				foreach (var jobNode in jobList)
				{
					var job = this.ConstructJob(jobNode, site);
					this.ProgressBarColor = ProgressBarGreen;
					this.jobStarted = DateTime.UtcNow;
					this.StatusWriteLine("Starting " + jobNode.DisplayText);
					try
					{
						site.EditingEnabled = this.editingEnabled; // Reset every time, in case a job has manually set the site's value.
						await Task.Run(job.Execute).ConfigureAwait(false);
						this.completedJobs++;
					}
					catch (OperationCanceledException)
					{
						success = false;
						MessageBox.Show(Resources.JobCancelled, nameof(HoodBot), MessageBoxButton.OK, MessageBoxImage.Information);
						break;
					}
#pragma warning disable CA1031 // Do not catch general exception types
					catch (Exception e)
					{
						success = false;
						MessageBox.Show(e.GetType().Name + ": " + e.Message, e.Source, MessageBoxButton.OK, MessageBoxImage.Error);
						Debug.WriteLine(e.StackTrace);
						break;
					}
#pragma warning restore CA1031 // Do not catch general exception types
				}

				jobRunner?.OnJobsCompleted(success);

				this.ResetSite(site);
				this.pauser = null;
				this.canceller = null;
			}

			this.Reset();
			this.StatusWriteLine("Total time for last run: " + FormatTimeSpan(allJobsTimer.Elapsed));
			this.executing = false;
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
			var user = this.UserName ?? wikiInfo.UserName;
			var password = this.Password ?? wikiInfo.Password ?? throw new InvalidOperationException(Resources.PasswordNotSet);
			if (user != null)
			{
				site.Login(user, password);
			}

			return site;
		}

		private void JobTree_OnSelectionChanged(TreeNode sender, SelectedItemChangedEventArgs e)
		{
			var main = App.Locator.MainWindow;
			var parameterControl = main.JobParameters;

			// TODO: Consider changing to attached property to be fully MVVM compliant.
			parameterControl.Children.Clear();
			parameterControl.RowDefinitions.Clear();

			var visibility = Visibility.Hidden;
			var enabled = false;
			if (e.Node is JobNode job)
			{
				enabled = job.IsChecked == true;
				var parameters = job.JobInfo.Parameters;
				if (e.Selected)
				{
					if (parameters.Count > 0)
					{
						visibility = Visibility.Visible;
						// If the box is already visible, this is a duplicate call arising from a check then a select, so skip it.
						if (this.JobParameterVisibility == Visibility.Hidden)
						{
							this.GetParametersFor(job);
						}
					}
					else if (job.Children != null)
					{
						foreach (var childNode in job.Children)
						{
							this.GetParametersFor((JobNode)childNode);
						}
					}
				}
				else
				{
					if (this.JobParameterVisibility == Visibility.Visible)
					{
						this.SetParametersOn(job);
						foreach (var param in parameters)
						{
							main.UnregisterName(param.Name);
						}
					}
				}
			}

			this.JobParametersEnabled = enabled;
			this.JobParameterVisibility = visibility;
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

			site.AbstractionLayer.WarningOccurred -= WalWarningOccurred;
			site.WarningOccurred -= SiteWarningOccurred;
#endif
			site.PagePreview -= this.SitePagePreview;
		}

		private void RunTest()
		{
		}

		private void SitePagePreview(Site sender, PagePreviewArgs eventArgs)
		{
			// Until we get a menu going, specify manually.
			// currentViewer ??= this.FindPlugin<IDiffViewer>("IeDiff");
			if (this.DiffViewer != null && this.ShowDiffs && sender.AbstractionLayer is ITokenGenerator tokens)
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
				this.DiffViewer.Compare(diffContent);
				this.DiffViewer.Wait();
			}
		}

		private void StatusWrite(string text) => this.Status += (this.Status?.Length ?? 0) == 0 ? text.TrimStart() : text;

		private void StatusWriteLine(string text) => this.StatusWrite(text + NewLine);
		#endregion
	}
}