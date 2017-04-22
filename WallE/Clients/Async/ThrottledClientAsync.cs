#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (file is not currently being maintained)
namespace RobinHood70.WallE.Clients.Async
{
	using System;
	using System.Diagnostics;
	using System.Net;
	using System.Net.Http;
	using System.Threading.Tasks;

	public class ThrottledClientAsync : IFormClientAsync
	{
		#region Fields
		private IFormClientAsync baseClient;
		private bool firstRequest = true;
		private Stopwatch stopwatch;
		#endregion

		#region Constructors
		public ThrottledClientAsync(IFormClientAsync baseClient)
			: this(baseClient, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(6))
		{
		}

		public ThrottledClientAsync(IFormClientAsync baseClient, TimeSpan readInterval, TimeSpan writeInterval)
		{
			this.baseClient = baseClient;
			this.stopwatch = new Stopwatch();
			this.stopwatch.Start();

			this.ReadInterval = readInterval;
			this.WriteInterval = writeInterval;
		}
		#endregion

		#region Public Events
		public event StrongEventHandler<IFormClientAsync, DelayEventArgs> RequestingDelay
		{
			add { this.baseClient.RequestingDelay += value; }
			remove { this.baseClient.RequestingDelay -= value; }
		}
		#endregion

		#region Public Properties
		public bool LastWasPost { get; private set; }

		public TimeSpan ReadInterval { get; set; }

		public TimeSpan WriteInterval { get; set; }
		#endregion

		#region Public Methods
		public async Task<string> GetAsync(Uri uri)
		{
			await this.Throttle().ConfigureAwait(false);
			var retval = await this.baseClient.GetAsync(uri).ConfigureAwait(false);
			this.stopwatch.Restart();
			this.LastWasPost = false;

			return retval;
		}

		public CookieContainer LoadCookies() => this.baseClient.LoadCookies();

		public async Task<string> PostAsync(Uri uri, HttpContent content)
		{
			await this.Throttle().ConfigureAwait(false);
			var retval = await this.baseClient.PostAsync(uri, content).ConfigureAwait(false);
			this.stopwatch.Restart();
			this.LastWasPost = false;

			return retval;
		}

		public Task<bool> RequestDelayAsync(TimeSpan delayTime, DelayReason reason) => this.baseClient.RequestDelayAsync(delayTime, reason);

		public void SaveCookies() => this.baseClient.SaveCookies();
		#endregion

		#region Private Methods
		private Task<bool> Throttle()
		{
			if (this.firstRequest)
			{
				this.firstRequest = false;
			}
			else
			{
				var delayTime = this.LastWasPost ? this.WriteInterval : this.ReadInterval;
				if (delayTime > this.stopwatch.Elapsed)
				{
					// If delayInterval is zero, this will be skipped automatically because StopWatch.Elapsed must be at least 0, so no need to check for that.
					return this.RequestDelayAsync(delayTime - this.stopwatch.Elapsed, DelayReason.ClientThrottled);
				}
			}

			return Task.FromResult(true);
		}
		#endregion
	}
}
