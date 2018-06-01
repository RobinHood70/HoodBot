namespace RobinHood70.WallE.Clients
{
	using System;
	using System.Diagnostics;
	using WikiCommon;

	/// <summary>This class wraps around any other <see cref="IMediaWikiClient"/> providing simple throttling based on whether the previous request was a GET request or a POST request.</summary>
	/// <seealso cref="IMediaWikiClient" />
	public class ThrottledClient : IMediaWikiClient
	{
		#region Fields
		private readonly IMediaWikiClient baseClient;
		private readonly Stopwatch stopwatch = new Stopwatch();
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ThrottledClient"/> class with default read and write intervals of 1 and 6 seconds, respectively).</summary>
		/// <param name="baseClient">The base client.</param>
		public ThrottledClient(IMediaWikiClient baseClient)
			: this(baseClient, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6))
		{
		}

		/// <summary>Initializes a new instance of the <see cref="ThrottledClient"/> class with specified read and write intervals.</summary>
		/// <param name="baseClient">The base client.</param>
		/// <param name="readInterval">The read interval.</param>
		/// <param name="writeInterval">The write interval.</param>
		public ThrottledClient(IMediaWikiClient baseClient, TimeSpan readInterval, TimeSpan writeInterval)
		{
			this.baseClient = baseClient;
			this.ReadInterval = readInterval;
			this.WriteInterval = writeInterval;
			this.stopwatch.Start();
		}
		#endregion

		#region Public Events

		/// <summary>The event raised when either the site or the client is requesting a delay.</summary>
		public event StrongEventHandler<IMediaWikiClient, DelayEventArgs> RequestingDelay
		{
			add { this.baseClient.RequestingDelay += value; }
			remove { this.baseClient.RequestingDelay -= value; }
		}
		#endregion

		#region Public Properties

		/// <summary>Gets a value indicating whether the last request was a POST request.</summary>
		/// <value><c>true</c> if the last request was a POST request; otherwise, <c>false</c>.</value>
		public bool? LastWasPost { get; private set; }

		/// <summary>Gets or sets the minimum time to wait between a GET request and the request after it.</summary>
		/// <value>The minimum time to wait between a GET request and the request after it.</value>
		public TimeSpan ReadInterval { get; set; }

		/// <summary>Gets or sets the miniumum time to wait between a POST request and the request after it.</summary>
		/// <value>The miniumum time to wait between a POST request and the request after it.</value>
		public TimeSpan WriteInterval { get; set; }
		#endregion

		#region Public Methods

		/// <summary>Deletes all cookies from persistent storage and clears the cookie cache.</summary>
		public void DeleteCookies() => this.baseClient.DeleteCookies();

		/// <summary>Downloads a file directly to disk instead of returning it as a string.</summary>
		/// <param name="uri">The URI to download from.</param>
		/// <param name="fileName">The filename to save to.</param>
		public void DownloadFile(Uri uri, string fileName)
		{
			this.Throttle();
			this.baseClient.DownloadFile(uri, fileName);
			this.stopwatch.Restart();
			this.LastWasPost = false;
		}

		/// <summary>Gets the text of the result returned by the given URI.</summary>
		/// <param name="uri">The URI to get.</param>
		/// <returns>The text of the result.</returns>
		public string Get(Uri uri)
		{
			this.Throttle();
			var retval = this.baseClient.Get(uri);
			this.stopwatch.Restart();
			this.LastWasPost = false;

			return retval;
		}

		/// <summary>Retrieves cookies from persistent storage.</summary>
		public void LoadCookies() => this.baseClient.LoadCookies();

		/// <summary>POSTs text data and retrieves the result.</summary>
		/// <param name="uri">The URI to POST data to.</param>
		/// <param name="postData">The text to POST.</param>
		/// <returns>The text of the result.</returns>
		public string Post(Uri uri, string postData)
		{
			this.Throttle();
			var retval = this.baseClient.Post(uri, postData);
			this.stopwatch.Restart();
			this.LastWasPost = false;

			return retval;
		}

		/// <summary>POSTs byte data and retrieves the result.</summary>
		/// <param name="uri">The URI to POST data to.</param>
		/// <param name="contentType">The text of the content type. Typicially "<c>x-www-form-urlencoded</c>" or "<c>multipart/form-data ...</c>", but there is no restriction on values.</param>
		/// <param name="postData">The byte array to POST.</param>
		/// <returns>The text of the result.</returns>
		public string Post(Uri uri, string contentType, byte[] postData)
		{
			this.Throttle();
			var retval = this.baseClient.Post(uri, contentType, postData);
			this.stopwatch.Restart();
			this.LastWasPost = false;

			return retval;
		}

		/// <summary>This method is used both to throttle clients as well as to forward any wiki-requested delays, such as from maxlag. Clients should respect any delays requested by the wiki unless they expect to abort the procedure, or for testing.</summary>
		/// <param name="delayTime">The amount of time to delay for.</param>
		/// <param name="reason">The reason for the delay, as specified by the caller.</param>
		/// <returns>A value indicating whether or not the delay was respected.</returns>
		public bool RequestDelay(TimeSpan delayTime, DelayReason reason) => this.baseClient.RequestDelay(delayTime, reason);

		/// <summary>Saves all cookies to persistent storage.</summary>
		public void SaveCookies() => this.baseClient.SaveCookies();
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => nameof(ThrottledClient);
		#endregion

		#region Private Methods
		private bool Throttle()
		{
			var delayTime =
				this.LastWasPost == null ? TimeSpan.Zero :
				(this.LastWasPost.Value ? this.WriteInterval : this.ReadInterval) - this.stopwatch.Elapsed;
			return delayTime > TimeSpan.Zero ? this.RequestDelay(delayTime, DelayReason.ClientThrottled) : true;
		}
		#endregion
	}
}
