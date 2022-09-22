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
	using RobinHood70.CommonCode;

	public class SimpleClient : IMediaWikiClient, IDisposable
	{
		#region Fields
		private readonly CancellationToken cancellationToken;
		private readonly CookieContainer cookieContainer = new();
		private readonly string? cookiesLocation;
		private readonly SimpleClientRetryHandler retryHandler;
		private readonly HttpClient httpClient;
		private readonly HttpClientHandler webHandler;
		private bool disposed;
		#endregion

		#region Constructors
		public SimpleClient(CancellationToken cancellationToken)
			: this(null, null, cancellationToken)
		{
		}

		public SimpleClient(string cookiesLocation, CancellationToken cancellationToken)
			: this(null, cookiesLocation, cancellationToken)
		{
		}

		public SimpleClient(string? contactInfo, string? cookiesLocation, CancellationToken cancellationToken)
		{
			ServicePointManager.Expect100Continue = false;
			this.UserAgent = ClientShared.BuildUserAgent(contactInfo);
			this.cookiesLocation = cookiesLocation;
			this.LoadCookies();
			this.cancellationToken = cancellationToken;

			// See http://stackoverflow.com/questions/8739065/using-object-initializer-generates-ca-2000-warning for why we're not using an object initializer.
			this.webHandler = new()
			{
				AllowAutoRedirect = true,
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				CookieContainer = this.cookieContainer,
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

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>Downloads a file directly to disk instead of returning it as a string.</summary>
		/// <param name="uri">The URI to download from.</param>
		/// <param name="fileName">The filename to save to.</param>
		/// <returns><see langword="true"/> if the download succeeded; otherwise <see langword="false"/>.</returns>
		public bool DownloadFile(Uri uri, string fileName)
		{
			uri.ThrowNull();
			fileName.ThrowNull();
			using HttpRequestMessage request = new(HttpMethod.Get, uri);
			var maxLag = this.HonourMaxLag;
			this.HonourMaxLag = false;
			using var response = this.httpClient.Send(request, this.cancellationToken);
			{
				var data = GetResponseData(response);
				if (data.Length > 0)
				{
					try
					{
						File.WriteAllBytes(fileName, data);
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
					finally
					{
						this.HonourMaxLag = maxLag;
					}
				}
			}

			this.SaveCookies();
			this.HonourMaxLag = maxLag;
			return false;
		}

		/// <inheritdoc/>
		public void ExpireAll()
		{
#if NET6_0
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

		#region Private Methods

		private static byte[] GetResponseData(HttpResponseMessage response)
		{
			using var respStream = response.Content.ReadAsStream();
			using MemoryStream mem = new();
			respStream.CopyTo(mem);
			return mem.ToArray();
		}

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
					if (JsonConvert.DeserializeObject<CookieCollection>(cookieText) is CookieCollection cookies)
					{
						this.cookieContainer.Add(cookies);
					}
				}
				catch (DirectoryNotFoundException)
				{
				}
				catch (FileNotFoundException)
				{
					/*
					var oldFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "HoodBot", "cookies.dat");
					if (File.Exists(oldFile))
					{
						using var stream = File.OpenRead(oldFile);
						var formatter = new BinaryFormatter() { Binder = new CookieBinder() };
						if (formatter.Deserialize(stream) is CookieContainer cookies)
						{
							this.cookieContainer = cookies;
						}
					}
					*/
				}
			}
		}

		/// <summary>Saves all cookies to persistent storage.</summary>
		private void SaveCookies()
		{
			if (this.cookiesLocation is not null)
			{
				var jsonCookies = JsonConvert.SerializeObject(this.cookieContainer.GetAllCookies());
				File.WriteAllText(this.cookiesLocation, jsonCookies);
			}
		}
		#endregion
	}
}