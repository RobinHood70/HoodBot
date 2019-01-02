namespace RobinHood70.HoodBot.ViewModel
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
	using RobinHood70.HoodBot.Jobs;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Eve;
	using static System.Environment;
	using static RobinHood70.HoodBot.Properties.Resources;
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

		private CancellationTokenSource canceller;
		private double completedJobs;
		private WikiInfo currentItem;
		private DateTime? eta;
		private bool executing;
		private Visibility jobParameterVisibility = Visibility.Hidden;
		private DateTime jobStarted;
		private double overallProgress;
		private double overallProgressMax = 1;
		private string password;
		private PauseTokenSource pauser;
		private WikiInfo previousItem;
		private Brush progressBarColor = ProgressBarGreen;
		private Site site;
		private string status;
		#endregion

		#region Constructors
		public MainViewModel()
		{
			this.appDataFolder = Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.Create), nameof(HoodBot));
			this.client = new SimpleClient(ContactInfo, Path.Combine(this.appDataFolder, "Cookies.dat"));
			this.client.RequestingDelay += this.Client_RequestingDelay;
			this.BotSettings = BotSettings.Load(Path.Combine(this.appDataFolder, "Settings.json"));
			this.CurrentItem = this.BotSettings.LastSelectedWiki;

			this.progressMonitor = new Progress<double>(this.ProgressChanged);
			this.statusMonitor = new Progress<string>(this.StatusChanged);
		}
		#endregion

		#region Destructor
		~MainViewModel()
		{
			this.BotSettings.Save();
			if (this.site != null)
			{
				(this.site.AbstractionLayer as WikiAbstractionLayer).SendingRequest -= WalSendingRequest;
				(this.site.AbstractionLayer as WikiAbstractionLayer).WarningOccurred -= WalWarningOccurred;
			}

			this.client.RequestingDelay -= this.Client_RequestingDelay;
		}
		#endregion

		#region Public Properties
		public WikiInfo CurrentItem
		{
			get => this.currentItem;
			set
			{
				if (this.Set(ref this.currentItem, value, nameof(this.CurrentItem)))
				{
					this.BotSettings.UpdateLastSelected(value);
				}
			}
		}

		public RelayCommand EditSettings => new RelayCommand(this.OpenEditWindow);

		public DateTime? Eta => this.eta?.ToLocalTime();

		public Visibility JobParameterVisibility
		{
			get => this.jobParameterVisibility;
			private set => this.Set(ref this.jobParameterVisibility, value, nameof(this.JobParameterVisibility));
		}

		public JobNode JobTree { get; } = new JobNode();

		public BotSettings BotSettings { get; }

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

		public RelayCommand Pause => new RelayCommand(this.PauseJobs);

		public Brush ProgressBarColor
		{
			get => this.progressBarColor;
			set => this.Set(ref this.progressBarColor, value, nameof(this.ProgressBarColor));
		}

		public string Status
		{
			get => this.status;
			set => this.Set(ref this.status, value, nameof(this.Status));
		}

		public RelayCommand Stop => new RelayCommand(this.CancelJobs);

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
		internal IParameterFetcher ParameterFetcher { get; set; }
		#endregion

		#region Public Static Methods (DEBUG only)
#if DEBUG
		public static void WalResponseRecieved(IWikiAbstractionLayer sender, ResponseEventArgs eventArgs) => Debug.WriteLine(sender + " Response: " + eventArgs.Response);

		public static void WalSendingRequest(IWikiAbstractionLayer sender, RequestEventArgs eventArgs) => Debug.WriteLine(eventArgs.Request.ToString(), sender.ToString());

		public static void WalWarningOccurred(IWikiAbstractionLayer sender, WallE.Design.WarningEventArgs eventArgs) => Debug.WriteLine($"Warning ({eventArgs.Warning.Code}): {eventArgs.Warning.Info}", sender.ToString());
#endif
		#endregion

		#region Internal Methods
		internal void GetParameters(JobNode jobNode)
		{
			if (jobNode?.Constructor == null)
			{
				return;
			}

			if (jobNode.Parameters == null)
			{
				jobNode.InitializeParameters();
			}

			this.JobParameterVisibility = jobNode.Parameters?.Count > 0 ? Visibility.Visible : Visibility.Hidden;
			foreach (var param in jobNode.Parameters)
			{
				this.ParameterFetcher.GetParameter(param);
			}
		}

		internal void SetParameters(JobNode jobNode)
		{
			if (jobNode?.Constructor == null || ((jobNode.Parameters?.Count ?? 0) == 0))
			{
				return;
			}

			foreach (var param in jobNode.Parameters)
			{
				this.ParameterFetcher.SetParameter(param);
			}
		}
		#endregion

		#region Private Static Methods
		private static string FormatTimeSpan(TimeSpan allJobsTimer)
		{
			var retval = allJobsTimer.ToString(@"h\h\ mm\m\ ss\.f", CultureInfo.CurrentCulture);
			retval = retval.TrimStart('0', ':', 'h', 'm', ' ').TrimEnd('0', '.');
			if (retval.Length == 0 || retval[0] == '.')
			{
				retval = '0' + retval;
			}

			return retval + 's';
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
			this.StatusChanged(CurrentCulture(DelayRequested, eventArgs.Reason, eventArgs.DelayTime, eventArgs.Description) + NewLine);
			App.WpfYield();
		}

		private WikiJob ConstructJob(JobNode jobNode)
		{
			var objectList = new List<object>
			{
				this.site,
				new AsyncInfo(this.progressMonitor, this.statusMonitor, this.pauser.Token, this.canceller.Token)
			};

			foreach (var param in jobNode.Parameters)
			{
				if (param.Attribute is JobParameterFileAttribute && param.Value is string value)
				{
					value = value.Replace("%BotData%", this.BotSettings.BotDataFolder);
					objectList.Add(ExpandEnvironmentVariables(value));
				}
				else
				{
					objectList.Add(param.Value);
				}
			}

			return jobNode.Constructor.Invoke(objectList.ToArray()) as WikiJob;
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
			this.InitializeSite();
			this.completedJobs = 0;
			this.OverallProgressMax = jobList.Count;
			using (var cancelSource = new CancellationTokenSource())
			{
				this.canceller = cancelSource;
				this.pauser = new PauseTokenSource();

				foreach (var jobNode in jobList)
				{
					var job = this.ConstructJob(jobNode);
					this.ProgressBarColor = ProgressBarGreen;
					this.jobStarted = DateTime.UtcNow;
					try
					{
						await Task.Run(job.Execute).ConfigureAwait(false);
						this.completedJobs++;
					}
					catch (OperationCanceledException)
					{
						MessageBox.Show(JobCancelled, nameof(HoodBot), MessageBoxButton.OK, MessageBoxImage.Information);
						break;
					}
					catch (Exception e)
					{
						MessageBox.Show(e.GetType().Name + ": " + e.Message, e.Source, MessageBoxButton.OK, MessageBoxImage.Error);
						Debug.WriteLine(e.StackTrace);
						break;
					}
				}

				this.pauser = null;
				this.canceller = null;
			}

			this.Reset();
			this.StatusChanged("Total time for last run: " + FormatTimeSpan(allJobsTimer.Elapsed));
			this.executing = false;
		}

		private List<JobNode> GetJobList()
		{
			var jobList = new List<JobNode>(this.JobTree.GetCheckedJobs());
			if (jobList.Count > 1)
			{
				var equalityComparer = new JobConstructorEqualityComparer();

				// Remove any duplicate jobs based on Constructor equality. Simple bubble-sort style algorithm is sufficient due to small size.
				for (var outerLoop = 0; outerLoop < jobList.Count - 1; outerLoop++)
				{
					for (var innerLoop = jobList.Count - 1; innerLoop > outerLoop; innerLoop--)
					{
						if (equalityComparer.Equals(jobList[innerLoop], jobList[outerLoop]))
						{
							jobList.RemoveAt(innerLoop);
						}
					}
				}
			}

			return jobList;
		}

		private void InitializeSite()
		{
			var wikiInfo = this.CurrentItem;
			if (wikiInfo == null)
			{
				throw new InvalidOperationException("No wiki has been selected.");
			}

			if (wikiInfo != this.previousItem)
			{
				if (this.site != null)
				{
					(this.site.AbstractionLayer as WikiAbstractionLayer).SendingRequest -= WalSendingRequest;
					(this.site.AbstractionLayer as WikiAbstractionLayer).WarningOccurred -= WalWarningOccurred;
				}

				this.previousItem = wikiInfo;
				var wal = new WikiAbstractionLayer(this.client, wikiInfo.Api)
				{
					MaxLag = wikiInfo.MaxLag
				};

				// wal.SendingRequest += WalSendingRequest;
				// wal.ResponseReceived += WalResponseRecieved;
				wal.WarningOccurred += WalWarningOccurred;
				this.site = new Site(wal);
				this.site.Login(wikiInfo.UserName, this.Password ?? wikiInfo.Password);
			}
		}

		private void OpenEditWindow()
		{
			var editWindow = new EditSettings();
			var view = editWindow.DataContext as SettingsViewModel;
			view.Client = this.client;
			view.BotSettings = this.BotSettings;
			view.CurrentItem = this.BotSettings.LastSelectedWiki;
			editWindow.ShowDialog();

			this.CurrentItem = this.BotSettings.LastSelectedWiki;
		}

		private void PauseJobs() => this.PauseJobs(!this.pauser.IsPaused);

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

		private void StatusChanged(string text) => this.Status += text;
		#endregion
	}
}