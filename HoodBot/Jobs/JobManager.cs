namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Loggers;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.HoodBot.Properties;
	using RobinHood70.HoodBotPlugins;
	using RobinHood70.Robby;
	using RobinHood70.WallE.Base;
	using RobinHood70.WallE.Clients;
	using RobinHood70.WallE.Eve;

	public class JobManager : IDisposable
	{
		#region Fields
		private readonly WikiInfo wikiInfo;
		private bool disposedValue;
		#endregion

		#region Constructors
		public JobManager(WikiInfo wikiInfo, PauseTokenSource pauseSource, CancellationTokenSource cancelSource)
		{
			this.wikiInfo = wikiInfo;
			this.CancelToken = cancelSource.Token;
			this.PauseToken = pauseSource.Token;
			this.Client = this.CreateClient();
			this.AbstractionLayer = this.CreateAbstractionLayer();
			this.Site = this.CreateSite();
		}
		#endregion

		#region Public Events
		public event StrongEventHandler<JobManager, bool>? FinishedAllJobs;

		public event StrongEventHandler<JobManager, JobEventArgs>? FinishedJob;

		public event StrongEventHandler<JobManager, DiffContent>? PagePreview;

		public event StrongEventHandler<JobManager, double>? ProgressUpdated;

		public event StrongEventHandler<JobManager, EventArgs>? StartingAllJobs;

		public event StrongEventHandler<JobManager, JobEventArgs>? StartingJob;

		public event StrongEventHandler<JobManager, string?>? StatusUpdated;
		#endregion

		#region Public Properties
		public IWikiAbstractionLayer AbstractionLayer { get; }

		public CancellationToken CancelToken { get; init; }

		public IMediaWikiClient Client { get; }

		public JobLogger? Logger { get; set; }

		public PauseToken? PauseToken { get; }

		public ResultHandler? ResultHandler { get; set; }

		public bool ShowDiffs { get; set; } = true;

		public Site Site { get; }
		#endregion

		#region Public Methods

		public void Dispose()
		{
			this.Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		public async Task Run(IEnumerable<JobInfo> jobList)
		{
			this.OnStartingAllJobs();
			var allSuccessful = true;
			var editingEnabledMaster = this.Site.EditingEnabled;
			var allJobsTimer = Stopwatch.StartNew();
			foreach (var jobInfo in jobList.NotNull())
			{
				var abort = this.OnStartingJob(jobInfo);
				if (abort)
				{
					// Unclear why subscriber would abort a job that hasn't even been attempted yet, but since we're using the same semantics for both starting and finishing a job, if subscriber asks for it, do it.
					break;
				}

				var job = jobInfo.Instantiate(this);
				try
				{
					await Task.Run(job.Execute, this.CancelToken).ConfigureAwait(true);
					abort = this.OnFinishedJob(jobInfo, null);
					if (abort)
					{
						break;
					}
				}
				catch (Exception e)
				{
					allSuccessful = false;
					abort = this.OnFinishedJob(jobInfo, e);
					if (abort)
					{
						throw;
					}
				}
				finally
				{
					// Reset value in case job cheated and changed it.
					this.Site.EditingEnabled = editingEnabledMaster;
				}
			}

			this.OnFinishedAllJobs(allSuccessful);
		}

		public void UpdateProgress(double progressPercent) => this.OnUpdateProgress(progressPercent);

		public void UpdateStatus(string? status) => this.OnUpdateStatus(status);
		#endregion

		#region Internal Static Methods

		// This is flagged as internal mostly to stop warnings whenever it's not in use.
		internal static string SiteName(IWikiAbstractionLayer sender) => sender.AllSiteInfo?.General?.SiteName ?? "Site-Agnostic";
		#endregion

		#region Protected Virtual Methods
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposedValue)
			{
				if (disposing)
				{
					this.DisposeSite();
					this.DisposeAbstractionLayer();
					this.DisposeClient();
				}

				this.disposedValue = true;
			}
		}

		protected virtual void OnFinishedAllJobs(bool allSuccessful)
		{
			this.Logger?.CloseLog();

			if (this.ResultHandler != null)
			{
				this.ResultHandler.Save();
				this.ResultHandler.Clear();
			}

			this.FinishedAllJobs?.Invoke(this, allSuccessful);
		}

		protected virtual bool OnFinishedJob(JobInfo job, Exception? e)
		{
			JobEventArgs eventArgs = new(job, e);
			this.FinishedJob?.Invoke(this, eventArgs);
			return eventArgs.Abort;
		}

		protected virtual void OnPagePreview(Site sender, PagePreviewArgs eventArgs)
		{
			// Until we get a menu going, specify manually.
			// currentViewer ??= this.FindPlugin<IDiffViewer>("IeDiff");
			if (sender.AbstractionLayer is ITokenGenerator tokens)
			{
				eventArgs.Token = tokens.TokenManager.SessionToken("csrf");
			}

			var page = eventArgs.Page;
			DiffContent diffContent = new(page.Title.FullPageName(), page.Text ?? string.Empty, eventArgs.EditSummary, eventArgs.Minor)
			{
				EditPath = page.EditPath,
				EditToken = eventArgs.Token,
				LastRevisionText = page.CurrentRevision?.Text,
				LastRevisionTimestamp = page.CurrentRevision?.Timestamp,
				StartTimestamp = page.StartTimestamp,
			};

			this.PagePreview?.Invoke(this, diffContent);
		}

		protected virtual bool OnStartingJob(JobInfo job)
		{
			JobEventArgs eventArgs = new(job, null);
			this.StartingJob?.Invoke(this, eventArgs);
			return eventArgs.Abort;
		}

		protected virtual void OnStartingAllJobs() => this.StartingAllJobs?.Invoke(this, EventArgs.Empty);

		protected virtual void OnUpdateProgress(double progressPercent) => this.ProgressUpdated?.Invoke(this, progressPercent);

		protected virtual void OnUpdateStatus(string? status) => this.StatusUpdated?.Invoke(this, status);

		protected virtual void SiteChanging(Site sender, ChangeArgs eventArgs)
		{
#if DEBUG
			if (!sender.EditingEnabled)
			{
				Debug.WriteLine($"{eventArgs.MethodName} (sender: {eventArgs.RealSender})");
				foreach (var parameter in eventArgs.Parameters)
				{
					Debug.WriteLine($"  {parameter.Key} = {parameter.Value}");
				}
			}
#endif
		}

		protected virtual void SiteWarningOccurred(Site sender, WarningEventArgs eventArgs) => Debug.WriteLine(eventArgs?.Warning);

		protected virtual void WalResponseRecieved(IWikiAbstractionLayer sender, ResponseEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Response: {eventArgs.Response}");

		protected virtual void WalSendingRequest(IWikiAbstractionLayer sender, RequestEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Request: {eventArgs.Request}");

		protected virtual void WalWarningOccurred(IWikiAbstractionLayer sender, WallE.Design.WarningEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Warning: ({eventArgs?.Warning.Code}) {eventArgs?.Warning.Info}");
		#endregion

		#region Private Methods
		private void Client_RequestingDelay(IMediaWikiClient sender, DelayEventArgs eventArgs)
		{
			if (eventArgs.Reason != DelayReason.ClientThrottled)
			{
				var text = Globals.CurrentCulture(
					Resources.DelayRequested,
					eventArgs.Reason,
					$"{eventArgs.DelayTime.TotalSeconds.ToString(CultureInfo.CurrentCulture)}s",
					eventArgs.Description);
				this.OnUpdateStatus(text + '\n');
			}
		}

		private IWikiAbstractionLayer CreateAbstractionLayer()
		{
			var api = this.wikiInfo.Api.PropertyNotNull(nameof(this.wikiInfo), nameof(this.wikiInfo.Api));
			IWikiAbstractionLayer abstractionLayer = string.Equals(api.OriginalString, "/", StringComparison.Ordinal)
				? new WallE.Test.WikiAbstractionLayer()
				: new WikiAbstractionLayer(this.Client, api);
			if (abstractionLayer is IMaxLaggable maxLagWal)
			{
				maxLagWal.MaxLag = this.wikiInfo.MaxLag ?? WikiInfo.DefaultMaxLag;
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

		private IMediaWikiClient CreateClient()
		{
			IMediaWikiClient client = new SimpleClient(App.UserSettings.ContactInfo, Path.Combine(App.UserFolder, "Cookies.json"), null, this.CancelToken);
			if (this.wikiInfo.ReadThrottling > 0 || this.wikiInfo.WriteThrottling > 0)
			{
				client = new ThrottledClient(
					client,
					TimeSpan.FromMilliseconds(this.wikiInfo.ReadThrottling ?? 0),
					TimeSpan.FromMilliseconds(this.wikiInfo.WriteThrottling ?? 1000));
			}

			client.RequestingDelay += this.Client_RequestingDelay;
			return client;
		}

		private Site CreateSite()
		{
			var retval = Site.GetFactoryMethod(this.wikiInfo.SiteClassIdentifier)(this.AbstractionLayer);
			retval.Changing += this.SiteChanging;
			retval.PagePreview += this.OnPagePreview;
			retval.WarningOccurred += this.SiteWarningOccurred;
			return retval;
		}

		private void DisposeAbstractionLayer()
		{
			this.AbstractionLayer.WarningOccurred -= this.WalWarningOccurred;
			if (this.AbstractionLayer is IInternetEntryPoint internet)
			{
				internet.SendingRequest -= this.WalSendingRequest;
			}
		}

		private void DisposeClient() => this.Client.RequestingDelay -= this.Client_RequestingDelay;

		private void DisposeSite()
		{
			this.Site.WarningOccurred -= this.SiteWarningOccurred;
			this.Site.PagePreview -= this.OnPagePreview;
			this.Site.Changing -= this.SiteChanging;
		}
		#endregion
	}
}