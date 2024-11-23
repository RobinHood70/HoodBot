namespace RobinHood70.WallE.Clients;

using System;
using System.Diagnostics;
using System.Net.Http;
using RobinHood70.CommonCode;

/// <summary>This class wraps around any other <see cref="IMediaWikiClient" /> providing simple throttling based on whether the previous request was a GET request or a POST request.</summary>
/// <seealso cref="IMediaWikiClient" />
/// <remarks>Initializes a new instance of the <see cref="ThrottledClient" /> class with specified read and write intervals.</remarks>
/// <param name="baseClient">The base client.</param>
/// <param name="readInterval">The read interval.</param>
/// <param name="writeInterval">The write interval.</param>
public class ThrottledClient(IMediaWikiClient baseClient, TimeSpan readInterval, TimeSpan writeInterval) : IMediaWikiClient
{
	#region Fields
	private readonly long readIntervalTicks = readInterval.Ticks;
	private readonly long writeIntervalTicks = writeInterval.Ticks;
	private long nextAllowed;
	#endregion

	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="ThrottledClient" /> class with default read and write intervals of 1 and 6 seconds, respectively).</summary>
	/// <param name="baseClient">The base client.</param>
	public ThrottledClient(IMediaWikiClient baseClient)
		: this(baseClient, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6))
	{
	}
	#endregion

	#region Public Events

	/// <summary>The event raised when either the site or the client is requesting a delay.</summary>
	public event StrongEventHandler<IMediaWikiClient, DelayEventArgs> DelayComplete
	{
		add => baseClient.DelayComplete += value;
		remove => baseClient.DelayComplete -= value;
	}

	/// <summary>The event raised when either the site or the client is requesting a delay.</summary>
	public event StrongEventHandler<IMediaWikiClient, DelayEventArgs> RequestingDelay
	{
		add => baseClient.RequestingDelay += value;
		remove => baseClient.RequestingDelay -= value;
	}
	#endregion

	#region Public Methods

	/// <summary>Deletes all cookies from persistent storage and clears the cookie cache.</summary>
	public void ExpireAll() => baseClient.ExpireAll();

	/// <summary>Downloads a file directly to disk instead of returning it as a string.</summary>
	/// <param name="uri">The URI to download from.</param>
	/// <param name="fileName">The filename to save to.</param>
	/// <returns><see langword="true"/> if the download succeeded; otherwise <see langword="false"/>.</returns>
	public bool DownloadFile(Uri uri, string? fileName)
	{
		this.Throttle();
		var retval = baseClient.DownloadFile(uri, fileName);
		this.nextAllowed = Stopwatch.GetTimestamp() + this.readIntervalTicks;

		return retval;
	}

	/// <summary>Gets the text of the result returned by the given URI.</summary>
	/// <param name="uri">The URI to get.</param>
	/// <returns>The text of the result.</returns>
	public string? Get(Uri uri)
	{
		this.Throttle();
		var retval = baseClient.Get(uri);
		this.nextAllowed = Stopwatch.GetTimestamp() + this.readIntervalTicks;

		return retval;
	}

	/// <inheritdoc/>
	public string? Post(Uri uri, HttpContent content)
	{
		this.Throttle();
		var retval = baseClient.Post(uri, content);
		this.nextAllowed = Stopwatch.GetTimestamp() + this.writeIntervalTicks;

		return retval;
	}

	/// <summary>This method is used both to throttle clients as well as to forward any wiki-requested delays, such as from maxlag. Clients should respect any delays requested by the wiki unless they expect to abort the procedure, or for testing.</summary>
	/// <param name="delayTime">The amount of time to delay for.</param>
	/// <param name="reason">The reason for the delay, as specified by the caller.</param>
	/// <param name="description">The human-readable reason for the delay, as specified by the caller.</param>
	/// <returns>A value indicating whether or not the delay was respected.</returns>
	public bool RequestDelay(TimeSpan delayTime, DelayReason reason, string description) => baseClient.RequestDelay(delayTime, reason, description);

	/// <inheritdoc/>
	public bool UriExists(Uri uri)
	{
		this.Throttle();
		var retval = baseClient.UriExists(uri);
		this.nextAllowed = Stopwatch.GetTimestamp() + this.readIntervalTicks;

		return retval;
	}
	#endregion

	#region Public Override Methods

	/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
	/// <returns>A <see cref="string" /> that represents this instance.</returns>
	public override string ToString() => nameof(ThrottledClient);
	#endregion

	#region Private Methods
	private void Throttle()
	{
		if (this.nextAllowed != 0)
		{
			var elapsed = Stopwatch.GetElapsedTime(this.nextAllowed);
			if (elapsed < TimeSpan.Zero)
			{
				this.RequestDelay(-elapsed, DelayReason.ClientThrottled, "Throttled");
			}
		}
	}
	#endregion
}