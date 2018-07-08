namespace RobinHood70.HoodBot.ViewModel
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Threading;
	using System.Windows;
	using System.Windows.Media;
	using RobinHood70.HoodBot.Jobs;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Eve;
	using static System.Environment;
	using static RobinHood70.HoodBot.Properties.Resources;

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
		private readonly string dataFolder;
		private readonly IProgress<double> progressMonitor;
		private readonly IProgress<string> statusMonitor;

		private double botPicOpacity = 1;
		private CancellationTokenSource canceller;
		private double completedJobs;
		private WikiInfo currentItem;
		private DateTime? eta;
		private bool executing;
		private DateTime jobStarted;
		private double overallProgress;
		private double overallProgressMax = 1;
		private string password;
		private PauseTokenSource pauser;
		private WikiInfo previousItem;
		private Brush progressBarColor = ProgressBarGreen;
		private Site site;
		#endregion

		#region Constructors
		public MainViewModel()
		{
			this.dataFolder = Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.Create), nameof(HoodBot));
			this.client = new SimpleClient(ContactInfo, Path.Combine(this.dataFolder, "Cookies.dat"));
			this.KnownWikis = WikiList.Load(Path.Combine(this.dataFolder, "WikiList.json"));
			this.CurrentItem = this.KnownWikis.LastSelectedItem;

			this.progressMonitor = new Progress<double>(this.ProgressChanged);
			this.statusMonitor = new Progress<string>(this.StatusChanged);
		}
		#endregion

		#region Destrctor
		~MainViewModel() => this.KnownWikis.Save();
		#endregion

		#region Public Properties
		public double BotPicOpacity
		{
			get => this.botPicOpacity;
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

		public JobNodeCollection JobTree { get; } = new JobNodeCollection();

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

		public RelayCommand Pause => new RelayCommand(this.PauseJobs);

		public Brush ProgressBarColor
		{
			get => this.progressBarColor;
			set => this.Set(ref this.progressBarColor, value, nameof(this.ProgressBarColor));
		}

		public RelayCommand Stop => new RelayCommand(this.CancelJobs);

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

		#region Internal Methods
		internal void GetParameters(JobNode jobNode)
		{
			if (jobNode.IsChecked == false)
			{
				return;
			}

			var jobParameters = jobNode.Parameters;
			if (jobParameters == null)
			{
				jobNode.InitializeParameters();
				jobParameters = jobNode.Parameters;
			}

			var wantedParameters = jobNode.Constructor.GetParameters();
			this.SetBotPicOpacity(wantedParameters.Length > 2);
			foreach (var parameter in wantedParameters)
			{
				var paramInfos = parameter.GetCustomAttributes(typeof(JobParameterAttribute), true);
				var paramInfo = (paramInfos.Length == 1 ? paramInfos[0] : null) as JobParameterAttribute;
				switch (parameter.ParameterType.Name)
				{
					case "Site":
					case "AsyncInfo":
						break;
					default:
						if (!jobParameters.TryGetValue(parameter.Name, out var value))
						{
							value = paramInfo.DefaultValue;
							jobParameters.Add(parameter.Name, value);
						}

						Debug.WriteLine($"Want parameter {parameter.Name} ({parameter.ParameterType.Name}={value})");

						break;
				}
			}
		}
		#endregion

		#region Private Methods
		private void CancelJobs()
		{
			this.canceller?.Cancel();
			this.Reset();
			this.PauseJobs(false);
		}

		private async void ExecuteJobs()
		{
			if (this.executing)
			{
				return;
			}

			this.executing = true;
			this.SetupWiki();
			this.completedJobs = 0;

			var equalityComparer = new JobConstructorEqualityComparer();
			var jobList = new List<JobNode>(this.JobTree.GetCheckedJobs());
			if (jobList.Count > 1)
			{
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

			this.OverallProgressMax = jobList.Count;

			using (var cancelSource = new CancellationTokenSource())
			{
				this.canceller = cancelSource;
				this.pauser = new PauseTokenSource();

				var job = new OneJob(this.site, new AsyncInfo(this.canceller.Token, this.pauser.Token, this.progressMonitor, this.statusMonitor));
				try
				{
					this.ProgressBarColor = ProgressBarGreen;
					this.jobStarted = DateTime.UtcNow;
					await job.Execute();
					this.completedJobs++;
					this.Reset();
				}
				catch (OperationCanceledException)
				{
					this.Reset();
					MessageBox.Show(JobCancelled, nameof(HoodBot), MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception e)
				{
					MessageBox.Show(e.GetType().Name + ": " + e.Message, e.Source, MessageBoxButton.OK, MessageBoxImage.Error);
					this.Reset(); // Deliberately placed after messagebox so approximate progress can be guaged.
				}
				finally
				{
					this.pauser = null;
					this.canceller = null;
					this.executing = false;
				}
			}
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

		private void PauseJobs() => this.PauseJobs(!this.pauser.IsPaused);

		private void PauseJobs(bool isPaused)
		{
			this.pauser.IsPaused = isPaused;
			this.ProgressBarColor = isPaused ? ProgressBarYellow : ProgressBarGreen;
		}

		private void ProgressChanged(double e)
		{
			this.OverallProgress = this.completedJobs + e;
			var timeDiff = DateTime.UtcNow - this.jobStarted;
			if (this.OverallProgress > 0 && timeDiff.TotalSeconds >= 5)
			{
				var completionTime = TimeSpan.FromTicks((long)(timeDiff.Ticks * this.OverallProgressMax / this.OverallProgress));
				this.UtcEta = this.jobStarted + completionTime;
			}
		}

		private void Reset()
		{
			this.OverallProgress = 0;
			this.UtcEta = null;
		}

		private void SetBotPicOpacity(bool hasControls) => this.BotPicOpacity = hasControls ? 0.3 : 1;

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

		private void StatusChanged(string obj) => throw new NotImplementedException();
		#endregion
	}
}