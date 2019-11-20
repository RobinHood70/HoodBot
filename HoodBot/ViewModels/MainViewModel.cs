namespace RobinHood70.HoodBot.ViewModels
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Media;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.HoodBot.Properties;
	using RobinHood70.HoodBot.Views;
	using RobinHood70.HoodBotPlugins;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using static System.Environment;
	using static RobinHood70.WikiCommon.Globals;

	public class MainViewModel : Notifier
	{
		#region Private Constants
		private const string ContactInfo = "robinhood70@live.ca";
		#endregion

		#region Static Fields
		private static readonly Brush ProgressBarGreen = new SolidColorBrush(Color.FromArgb(255, 6, 176, 37));
		private static readonly Brush ProgressBarYellow = new SolidColorBrush(Color.FromArgb(255, 255, 240, 0));
		#endregion

		#region Fields
		private readonly IMediaWikiClient client;
		private readonly string appDataFolder;
		private readonly IProgress<double> progressMonitor;
		private readonly IProgress<string> statusMonitor;

		private CancellationTokenSource? canceller;
		private double completedJobs;
		private WikiInfo? currentItem;
		private bool editingEnabled;
		private DateTime? eta;
		private bool executing;
		private Visibility jobParameterVisibility = Visibility.Hidden;
		private DateTime jobStarted;
		private double overallProgress;
		private double overallProgressMax = 1;
		private string? password;
		private PauseTokenSource? pauser;
		private Brush progressBarColor = ProgressBarGreen;
		private string status = string.Empty;
		#endregion

		#region Constructors
		public MainViewModel()
		{
			this.appDataFolder = Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.Create), nameof(HoodBot));
			this.client = new SimpleClient(ContactInfo, Path.Combine(this.appDataFolder, "Cookies.dat"));
			this.client.RequestingDelay += this.Client_RequestingDelay;
			this.BotSettings = BotSettings.Load(Path.Combine(this.appDataFolder, "Settings.json"));
			this.CurrentItem = this.BotSettings.GetCurrentItem();
			this.progressMonitor = new Progress<double>(this.ProgressChanged);
			this.statusMonitor = new Progress<string>(this.StatusWrite);
			Site.RegisterSiteClass(Uesp.UespSite.CreateInstance, "UespHoodBot");
			var plugins = Plugins.Instance;
			this.DiffViewer = plugins.DiffViewers["Internet Explorer"];
		}
		#endregion

		#region Destructor
		~MainViewModel()
		{
			this.BotSettings.Save();
			this.client.RequestingDelay -= this.Client_RequestingDelay;
		}
		#endregion

		#region Public Properties
		public BotSettings BotSettings { get; }

		public WikiInfo? CurrentItem
		{
			get => this.currentItem;
			set
			{
				if (this.currentItem != null)
				{
					this.BotSettings.UpdateCurrentWiki(value);
					this.BotSettings.Save();
				}

				this.Set(ref this.currentItem, value, nameof(this.CurrentItem));
			}
		}

		public IDiffViewer DiffViewer { get; set; }

		public bool EditingEnabled
		{
			get => this.editingEnabled;
			set => this.Set(ref this.editingEnabled, value, nameof(this.EditingEnabled));
		}

		public RelayCommand EditSettings => new RelayCommand(this.OpenEditWindow);

		public DateTime? Eta => this.eta?.ToLocalTime();

		public Visibility JobParameterVisibility
		{
			get => this.jobParameterVisibility;
			private set => this.Set(ref this.jobParameterVisibility, value, nameof(this.JobParameterVisibility));
		}

		public JobNode JobTree { get; } = new JobNode();

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

		public bool ShowDiffs { get; private set; } = true; // Simple hard-coded setting for now.

		public string Status
		{
			get => this.status;
			set => this.Set(ref this.status, value ?? string.Empty, nameof(this.Status));
		}

		public RelayCommand Stop => new RelayCommand(this.CancelJobs);

		public RelayCommand Test => new RelayCommand(this.RunTest);

		public DateTime? UtcEta
		{
			get => this.eta;
			private set
			{
				if (this.Set(ref this.eta, value, nameof(this.UtcEta)))
				{
					this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Eta)));
				}
			}
		}
		#endregion

		#region Internal Properties
		internal IParameterFetcher? ParameterFetcher { get; set; }
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
		internal void GetParameters(JobNode jobNode)
		{
			if (jobNode != null
				&& jobNode.Constructor != null
				&& this.ParameterFetcher != null
				&& (jobNode.Parameters ?? jobNode.InitializeParameters()) is IReadOnlyList<ConstructorParameter> jobParams)
			{
				this.JobParameterVisibility = jobParams.Count > 0 ? Visibility.Visible : Visibility.Hidden;
				foreach (var param in jobParams)
				{
					this.ParameterFetcher.GetParameter(param);
				}
			}
		}

		internal void SetParameters(JobNode jobNode)
		{
			if (jobNode != null
				&& jobNode.Constructor != null
				&& this.ParameterFetcher != null
				&& (jobNode.Parameters ?? jobNode.InitializeParameters()) is IReadOnlyList<ConstructorParameter> jobParams)
			{
				foreach (var param in jobParams)
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

		private WikiJob ConstructJob(JobNode jobNode, Site site)
		{
			ThrowNull(jobNode.Constructor, nameof(jobNode), nameof(jobNode.Constructor));
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
			var jobList = this.GetJobList();
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

				var success = true;
				var site = this.InitializeSite();
				var jobRunner = site as IJobAware;
				jobRunner?.OnJobsStarted();

				foreach (var jobNode in jobList)
				{
					var job = this.ConstructJob(jobNode, site);
					this.ProgressBarColor = ProgressBarGreen;
					this.jobStarted = DateTime.UtcNow;
					this.StatusWriteLine("Starting " + jobNode.Name);
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

		private List<JobNode> GetJobList()
		{
			var jobList = new List<JobNode>(JobNode.GetCheckedJobs(this.JobTree.Children));
			if (jobList.Count > 1)
			{
				// Remove any duplicate jobs based on Constructor equality (i.e., same job checked under multiple branches of tree). Simple nested loop algorithm is sufficient due to small size.
				for (var outerLoop = 0; outerLoop < jobList.Count - 1; outerLoop++)
				{
					for (var innerLoop = jobList.Count - 1; innerLoop > outerLoop; innerLoop--)
					{
						if (jobList[innerLoop].ConstructorEquals(jobList[outerLoop]))
						{
							jobList.RemoveAt(innerLoop);
						}
					}
				}
			}

			return jobList;
		}

		private Site InitializeSite()
		{
			var wikiInfo = this.CurrentItem ?? throw new InvalidOperationException(Resources.NoWiki);
			var abstractionLayer = wikiInfo.GetAbstractionLayer(this.client);

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
			if (wikiInfo.UserName != null)
			{
				site.Login(wikiInfo.UserName, this.Password ?? wikiInfo.Password ?? throw new InvalidOperationException(Resources.PasswordNotSet));
			}

			return site;
		}

		private void OpenEditWindow()
		{
			new EditSettings(this.BotSettings, this.client, this.CurrentItem).ShowDialog();
			this.CurrentItem = this.BotSettings.GetCurrentItem();
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