#pragma warning disable CS1591 // Missing XML comment for privately visible type or member (file is not currently being maintained)
namespace RobinHood70.WallE.Clients
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Net.Http;
	using System.Net.Http.Headers;
	using System.Security;
	using System.Threading;
	using System.Threading.Tasks;
	using Newtonsoft.Json;
	using Newtonsoft.Json.Serialization;
	using RobinHood70.CommonCode;

	public class SimpleClient : IMediaWikiClient, IDisposable
	{
		#region Fields
		private readonly CancellationToken cancellationToken;
		private readonly CookieContainer cookieContainer = new();
		private readonly string? cookiesLocation;
		private readonly SimpleClientRetryHandler retryHandler;
		private readonly HttpClient httpClient;
		private readonly JsonSerializerSettings jsonSettings = new();
		private readonly HttpClientHandler webHandler;
		private bool disposed;
		private DateTime cookiesLastUpdated = DateTime.MinValue;
		#endregion

		#region Constructors
		public SimpleClient(CancellationToken cancellationToken)
			: this(null, null, null, cancellationToken)
		{
		}

		public SimpleClient(string cookiesLocation, CancellationToken cancellationToken)
			: this(null, cookiesLocation, null, cancellationToken)
		{
		}

		public SimpleClient(string? contactInfo, string? cookiesLocation, ICredentials? credentials, CancellationToken cancellationToken)
		{
			ServicePointManager.Expect100Continue = false;
			this.UserAgent = ClientShared.BuildUserAgent(contactInfo);
			var resolver = new DefaultContractResolver
			{
				// Instructs Json to use fields instead of properties while (de-)serializing. This is necessary so that cookies retain their Timestamp property which is get-only, backed by a private field.
				IgnoreSerializableAttribute = false
			};
			this.jsonSettings.ContractResolver = resolver;
			this.cookiesLocation = cookiesLocation;
			this.LoadCookies();
			this.cancellationToken = cancellationToken;
			this.webHandler = new()
			{
				AllowAutoRedirect = true,
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				CookieContainer = this.cookieContainer,
				Credentials = credentials,
				UseCookies = true,
			};
			this.retryHandler = new SimpleClientRetryHandler(this, this.webHandler);

			HttpClient client = new(this.retryHandler);
			var headers = client.DefaultRequestHeaders;
			headers.UserAgent.ParseAdd(this.UserAgent);
			headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
			this.httpClient = client;
		}
		#endregion

		#region Events
		public event StrongEventHandler<IMediaWikiClient, DelayEventArgs>? DelayComplete;

		public event StrongEventHandler<IMediaWikiClient, DelayEventArgs>? RequestingDelay;
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a value indicating whether to honour maxlag requests.</summary>
		/// <value><see langword="true"/> if maxlag requests should be honoured; otherwise, <see langword="false"/>.</value>
		public bool HonourMaxLag { get; set; } = true;

		public int Retries { get; set; } = 3;

		public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(10);

		private string UserAgent { get; }
		#endregion

		#region Public Methods
		public void ClearCookies(Uri? uri)
		{
			var cookies = uri is null
				? this.cookieContainer.GetAllCookies()
				: this.cookieContainer.GetCookies(uri);
			foreach (var cookie in cookies as IEnumerable<Cookie>)
			{
				cookie.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(1));
			}
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>Downloads a file directly to disk instead of returning it as a string.</summary>
		/// <param name="uri">The URI to download from.</param>
		/// <param name="fileName">The filename to save to.</param>
		/// <returns><see langword="true"/> if the download succeeded; otherwise <see langword="false"/>.</returns>
		public bool DownloadFile(Uri uri, string? fileName)
		{
			ArgumentNullException.ThrowIfNull(uri);
			using HttpRequestMessage request = new(HttpMethod.Get, uri);
			var maxLag = this.HonourMaxLag;
			this.HonourMaxLag = false;
			using var response = this.httpClient.Send(request, this.cancellationToken);
			this.SaveCookies();
			this.HonourMaxLag = maxLag;
			if (!response.IsSuccessStatusCode)
			{
				return false;
			}

			try
			{
				using var outStream = fileName is null
					? Stream.Null
					: File.OpenWrite(fileName);
				response.Content.CopyTo(outStream, null, this.cancellationToken);
				return true;
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (NotSupportedException)
			{
			}
			catch (SecurityException)
			{
			}

			return false;
		}

		/// <inheritdoc/>
		public void ExpireAll()
		{
#if NET6_0_OR_GREATER
			foreach (var cookie in (IEnumerable<Cookie>)this.cookieContainer.GetAllCookies())
			{
				cookie.Expired = true;
			}
#endif
		}

		public string Get(Uri uri)
		{
			using HttpRequestMessage request = new(HttpMethod.Get, uri);
			using var response = this.httpClient.Send(request, this.cancellationToken);
			this.SaveCookies();
			return GetResponseText(response);
		}

		/// <inheritdoc/>
		public string Post(Uri uri, HttpContent content)
		{
			using HttpRequestMessage request = new(HttpMethod.Post, uri);
			request.Content = content;
			using var response = this.httpClient.Send(request, this.cancellationToken);
			this.SaveCookies();
			return GetResponseText(response);
		}

		/// <inheritdoc/>
		public bool RequestDelay(TimeSpan delayTime, DelayReason reason, string description)
		{
			if (delayTime <= TimeSpan.Zero)
			{
				return true;
			}

			DelayEventArgs e = new(delayTime, reason, description);
			this.OnRequestingDelay(e);
			if (e.Cancel)
			{
				return false;
			}

			Task.Delay(delayTime, this.cancellationToken).Wait(this.cancellationToken);
			this.OnDelayComplete(e);

			return true;
		}

		/// <inheritdoc/>
		public bool UriExists(Uri uri)
		{
			using HttpRequestMessage request = new(HttpMethod.Head, uri);
			using var response = this.httpClient.Send(request, this.cancellationToken);
			this.SaveCookies();
			return response.IsSuccessStatusCode;
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && !this.disposed)
			{
				this.webHandler.Dispose();
				this.retryHandler.Dispose();
				this.httpClient.Dispose();
			}

			this.disposed = true;
		}

		protected virtual void OnDelayComplete(DelayEventArgs e) => this.DelayComplete?.Invoke(this, e);

		protected virtual void OnRequestingDelay(DelayEventArgs e) => this.RequestingDelay?.Invoke(this, e);
		#endregion

		#region Private Static Methods
		private static DateTime LatestCookie(IReadOnlyCollection<Cookie> cookies)
		{
			var latest = DateTime.MinValue;
			foreach (var cookie in cookies)
			{
				if (cookie.TimeStamp > latest)
				{
					latest = cookie.TimeStamp;
				}
			}

			return latest;
		}
		#endregion

		#region Private Methods

		private static string GetResponseText(HttpResponseMessage response)
		{
			using var respStream = response.Content.ReadAsStream();
			using StreamReader reader = new(respStream);
			return reader.ReadToEnd();
		}

		private void LoadCookies()
		{
			if (this.cookiesLocation is not null)
			{
				try
				{
					var cookieText = File.ReadAllText(this.cookiesLocation);
					if (JsonConvert.DeserializeObject<CookieCollection>(cookieText, this.jsonSettings) is CookieCollection cookies)
					{
						this.cookieContainer.Add(cookies);
						this.cookiesLastUpdated = LatestCookie(cookies);
					}
				}
				catch (NullReferenceException)
				{
				}
				catch (DirectoryNotFoundException)
				{
				}
				catch (FileNotFoundException)
				{
				}
			}
		}

		/// <summary>Saves all cookies to persistent storage.</summary>
		private void SaveCookies()
		{
			if (this.cookiesLocation is not null)
			{
				var cookies = this.cookieContainer.GetAllCookies();
				var newLatestCookie = LatestCookie(cookies);
				if (newLatestCookie > this.cookiesLastUpdated)
				{
					this.cookiesLastUpdated = newLatestCookie;
					var settings = new JsonSerializerSettings();
					var resolver = new DefaultContractResolver
					{
						IgnoreSerializableAttribute = false
					};
					settings.ContractResolver = resolver;

					var jsonCookies = JsonConvert.SerializeObject(cookies, settings);
					File.WriteAllText(this.cookiesLocation, jsonCookies);
				}
			}
		}
		#endregion
	}
}