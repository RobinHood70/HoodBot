#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (file is not currently being maintained)
namespace RobinHood70.WallE.Clients.Async
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Net;
	using System.Net.Cache;
	using System.Net.Http;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Security;
	using System.Threading.Tasks;
	using RobinHood70.WallE.RequestBuilder;
	using static RobinHood70.Globals;

	public class SimpleClientAsync : IFormClientAsync, IDisposable
	{
		#region Fields
		private string cookiesLocation;
		private bool disposed;
		private SimpleClientAsyncRetryHandler handler;
		private HttpClient httpClient;
		private WebRequestHandler webHandler;
		#endregion

		#region Constructors
		public SimpleClientAsync()
			: this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cookies.dat"))
		{
		}

		public SimpleClientAsync(string cookiesLocation)
			: this(cookiesLocation, null)
		{
		}

		public SimpleClientAsync(string cookiesLocation, string contactInfo)
		{
			ServicePointManager.Expect100Continue = false;
			this.UserAgent = ClientShared.BuildUserAgent(contactInfo);
			this.cookiesLocation = cookiesLocation;
			var cookies = this.LoadCookies();

			// See http://stackoverflow.com/questions/8739065/using-object-initializer-generates-ca-2000-warning for why we're not using an object initializer.
#pragma warning disable IDE0017 // Simplify object initialization
			this.webHandler = new WebRequestHandler();
#pragma warning restore IDE0017 // Simplify object initialization
			this.webHandler.AllowAutoRedirect = true;
			this.webHandler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
			this.webHandler.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
			this.webHandler.CookieContainer = cookies;
			this.webHandler.ReadWriteTimeout = 6000;
			this.handler = new SimpleClientAsyncRetryHandler(this, this.webHandler);
			this.httpClient = new HttpClient(this.handler);
			this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(this.UserAgent);
		}
		#endregion

		#region Events
		public event StrongEventHandler<IFormClientAsync, DelayEventArgs> RequestingDelay;
		#endregion

		#region Public Properties
		public int Retries { get; set; } = 3;

		public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(10);

		public string UserAgent { get; set; }
		#endregion

		#region Public Methods
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		public async Task<string> GetAsync(Uri uri)
		{
			var response = await this.httpClient.GetAsync(uri).ConfigureAwait(false);
			var retval = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			return retval;
		}

		public CookieContainer LoadCookies()
		{
			// CookieContainer does not play well with serializers other than the BinaryFormatter (and reportedly SoapFormatter). Do not convert this to anything else without testing.
			if (this.cookiesLocation == null)
			{
				return new CookieContainer();
			}

			try
			{
				using (var stream = File.Open(this.cookiesLocation, FileMode.Open))
				{
					return new BinaryFormatter().Deserialize(stream) as CookieContainer;
				}
			}
			catch (FileNotFoundException)
			{
				Debug.WriteLine("Warning: Cookie container not found - using empty cookie container.");
				return new CookieContainer();
			}
		}

		// TODO: Needs to be moved to WikiAbstractionLayerAsync once that's developed
		public Task<string> GetResultAsync(Request request)
		{
			ThrowNull(request, nameof(request));

			// TODO: Possibly detect HSTS somewhere in here, but ONLY detection. Then bubble it up to the API or even just leave it alone and let the caller handle it per site.
			HttpResponseMessage response = null;
			try
			{
				switch (request.Type)
				{
					case RequestType.Post:
						using (var content = RequestVisitorHttpContentUrl.Build(request))
						{
							return this.PostAsync(request.Uri, content);
						}

					case RequestType.PostMultipart:
						using (var multipartData = RequestVisitorHttpContentMultipart.Build(request))
						{
							return this.PostAsync(request.Uri, multipartData);
						}

					default:
						var urib = new UriBuilder(request.Uri);
						using (var data = RequestVisitorHttpContentUrl.Build(request))
						{
							urib.Query = data.ReadAsStringAsync().Result;
							return this.GetAsync(urib.Uri);
						}
				}
			}
			finally
			{
				response?.Dispose();
			}
		}

		public async Task<string> PostAsync(Uri uri, HttpContent content)
		{
			var response = await this.httpClient.PostAsync(uri, content).ConfigureAwait(false);
			var retval = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			return retval;
		}

		/// <summary>This method is used both to throttle clients as well as to respect any wiki-requested delays, such as from maxlag. Clients should respect any delays requested unless they expect to abort the procedure or for testing.</summary>
		/// <param name="delayTime">The amount of time to delay for.</param>
		/// <param name="reason">The reason for the delay, as specified by the caller. At this point, the value is entirely arbitrary. This may be changed in the future.</param>
		/// <returns>A value indicating whether or not the delay was respected.</returns>
		public async Task<bool> RequestDelayAsync(TimeSpan delayTime, DelayReason reason)
		{
			if (delayTime <= TimeSpan.Zero)
			{
				return true;
			}

			var e = new DelayEventArgs(delayTime, reason);
			this.OnRequestingDelay(e);
			if (e.Cancel)
			{
				return false;
			}

			await Task.Delay(delayTime).ConfigureAwait(false);
			return true;
		}

		public void SaveCookies()
		{
			if (this.cookiesLocation != null)
			{
				FileStream stream = null;
				try
				{
					stream = File.Create(this.cookiesLocation);
					new BinaryFormatter().Serialize(stream, this.webHandler.CookieContainer);
				}
				catch (SystemException ex) when (ex is SecurityException)
				{
					Debug.WriteLine("Error saving cookies: " + ex.Message);
				}
				finally
				{
					stream?.Dispose();
				}
			}
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && !this.disposed)
			{
				this.webHandler.Dispose();
				this.webHandler = null;
				this.handler.Dispose();
				this.handler = null;
				this.httpClient.Dispose();
				this.httpClient = null;
			}

			this.disposed = true;
		}

		protected virtual void OnRequestingDelay(DelayEventArgs e) => this.RequestingDelay?.Invoke(this, e);
		#endregion
	}
}