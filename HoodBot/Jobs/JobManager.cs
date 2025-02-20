namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.Design;
using RobinHood70.HoodBot.Jobs.Loggers;
using RobinHood70.HoodBot.Models;
using RobinHood70.HoodBot.Properties;
using RobinHood70.HoodBotPlugins;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
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
	public JobManager(WikiInfo wikiInfo, bool editingEnabled, PauseTokenSource pauseSource, CancellationTokenSource cancelSource)
	{
		this.wikiInfo = wikiInfo;
		this.CancelToken = cancelSource.Token;
		this.PauseToken = pauseSource.Token;
		this.Client = this.CreateClient();
		this.AbstractionLayer = this.CreateAbstractionLayer();
		this.Site = this.CreateSite(this.wikiInfo, this.AbstractionLayer, editingEnabled);
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

	#region Public Static Methods

	public static void SiteChanging(Site sender, ChangeArgs eventArgs)
	{
#if DEBUG
		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(eventArgs);
		if (!sender.EditingEnabled)
		{
			Debug.WriteLine($"{eventArgs.MethodName} (sender: {eventArgs.RealSender})");
			foreach (var parameter in eventArgs.Parameters)
			{
				Debug.Write($"  {parameter.Key} = ");
				if (parameter.Value is string)
				{
					Debug.WriteLine(parameter.Value);
				}
				else if (parameter.Value is IEnumerable enumerable)
				{
					var first = true;
					foreach (var parameterValue in enumerable)
					{
						if (first)
						{
							first = false;
						}
						else
						{
							Debug.Write(", ");
						}

						Debug.Write(parameterValue.ToString());
					}

					Debug.WriteLine(string.Empty);
				}
				else
				{
					Debug.WriteLine(parameter.Value?.ToString());
				}
			}
		}
#endif
	}

	public static void SiteWarningOccurred(Site sender, WarningEventArgs eventArgs) => Debug.WriteLine(eventArgs?.Warning);

	public static void WalResponseRecieved(IWikiAbstractionLayer sender, ResponseEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Response: {eventArgs?.Response}");

	public static void WalSendingRequest(IWikiAbstractionLayer sender, RequestEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Request: {eventArgs?.Request}");

	public static void WalWarningOccurred(IWikiAbstractionLayer sender, WallE.Design.WarningEventArgs eventArgs) => Debug.WriteLine($"{SiteName(sender)} Warning: ({eventArgs?.Warning.Code}) {eventArgs?.Warning.Info}");
	#endregion

	#region Public Methods
	public Site CreateSite(WikiInfo wikiInfo, IWikiAbstractionLayer abstractionLayer, bool editingEnabled)
	{
		// TODO: Refactor OnPagePreview (and possibly others) so that CreateSite is no longer tied into JobManager and can be safely used from within a job like ImportBlocks. Should probably work as is for now, but is definitely sketchy.
		var retval = Site.GetFactoryMethod(wikiInfo.SiteClassIdentifier)(abstractionLayer);
		retval.EditingEnabled = editingEnabled;
		retval.Changing += SiteChanging;
		retval.PagePreview += this.OnPagePreview;
		retval.WarningOccurred += SiteWarningOccurred;
		return retval;
	}

	public void Dispose()
	{
		this.Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public void DisposeSite(Site site)
	{
		site.WarningOccurred -= SiteWarningOccurred;
		site.PagePreview -= this.OnPagePreview;
		site.Changing -= SiteChanging;
	}

	public void Login(string? userName, string? password, string? logPage, string? resultsPage)
	{
		// TODO: This could probably use a re-think to move logging in and such into the job itself. Then, each job could check if Site.LoggedIn or UserName or whatever and take appropriate action if needed. This would allow each job to customize login behaviour if needed, like log pages are already customizable, and we can do away with the custom attribute. Ultimately, all methods that actually access the site should occur within the job itself; this interim method just gets us a step closer.
		this.Site.Login(userName, password);

		// These must come after login since they require namespaces to be known.
		this.Logger = string.IsNullOrEmpty(logPage)
			? null
			: new PageJobLogger(TitleFactory.FromUnvalidated(this.Site, logPage));
		this.ResultHandler = string.IsNullOrEmpty(resultsPage)
			? null
			: new PageResultHandler(TitleFactory.FromUnvalidated(this.Site, resultsPage));
	}

	public async Task Run(IEnumerable<JobInfo> jobList)
	{
		ArgumentNullException.ThrowIfNull(jobList);

		this.OnStartingAllJobs();
		var allSuccessful = true;
		var editingEnabledMaster = this.Site.EditingEnabled;
		foreach (var jobInfo in jobList)
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
	internal static string SiteName(IWikiAbstractionLayer? sender) =>
		sender?.AllSiteInfo?.General?.SiteName ??
		(sender is IInternetEntryPoint net
			? net.EntryPoint.Host
			: "Site-Agnostic");
	#endregion

	#region Protected Virtual Methods
	protected virtual void Dispose(bool disposing)
	{
		if (!this.disposedValue)
		{
			if (disposing)
			{
				this.DisposeSite(this.Site);
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
		Globals.ThrowIfNull(this.wikiInfo.Api, nameof(JobManager), nameof(this.wikiInfo), nameof(this.wikiInfo.Api));
		var api = this.wikiInfo.Api;
		IWikiAbstractionLayer abstractionLayer = api.OriginalString.OrdinalEquals("/")
			? new WallE.Test.WikiAbstractionLayer()
			: new WikiAbstractionLayer(this.Client, api);
		if (abstractionLayer is IMaxLaggable maxLagWal)
		{
			maxLagWal.MaxLag = this.wikiInfo.MaxLag ?? WikiInfo.DefaultMaxLag;
		}

#if DEBUG
		if (abstractionLayer is IInternetEntryPoint internet)
		{
			internet.SendingRequest += WalSendingRequest;
			//// internet.ResponseReceived += WalResponseRecieved;
		}

		abstractionLayer.WarningOccurred += WalWarningOccurred;
#endif

		return abstractionLayer;
	}

	private IMediaWikiClient CreateClient()
	{
		// TODO: Below is a quick hack. Should probably be integrated into the UI at some point.
		NetworkCredential? credentials = null; // new NetworkCredential("user", "password");
		IMediaWikiClient client = new SimpleClient(App.UserSettings.ContactInfo, Path.Combine(App.UserFolder, "Cookies.json"), credentials, this.CancelToken);
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

	private void DisposeAbstractionLayer()
	{
		this.AbstractionLayer.WarningOccurred -= WalWarningOccurred;
		if (this.AbstractionLayer is IInternetEntryPoint internet)
		{
			internet.SendingRequest -= WalSendingRequest;
		}
	}

	private void DisposeClient() => this.Client.RequestingDelay -= this.Client_RequestingDelay;
	#endregion
}