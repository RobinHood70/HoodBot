namespace RobinHood70.WallE.Clients;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RobinHood70.CommonCode;

/// <summary>Provides a simple client to work with WallE.</summary>
public class SimpleClient : IMediaWikiClient, IDisposable
{
	#region Constants
	private const HashType CookieHashType = HashType.Md5;
	#endregion

	#region Static Fields
	private static readonly JsonSerializerSettings SerializerSettings = new()
	{
		ContractResolver = new DefaultContractResolver
		{
			IgnoreSerializableAttribute = false
		}
	};
	#endregion

	#region Fields
	private readonly CancellationToken cancellationToken;
	private readonly CookieContainer cookieContainer = new();
	private readonly string? cookiesLocation;
	private readonly SimpleClientRetryHandler retryHandler;
	private readonly HttpClient httpClient;
	private readonly JsonSerializerSettings jsonSettings = new();
	private readonly HttpClientHandler webHandler;
	private bool disposed;
	private string? previousHash;
	#endregion

	#region Constructors

	/// <summary>Initializes a new instance of the <see cref="SimpleClient"/> class.</summary>
	/// <param name="cancellationToken">A standard cancellation token that cancels any long operation.</param>
	public SimpleClient(CancellationToken cancellationToken)
		: this(null, null, null, cancellationToken)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="SimpleClient"/> class.</summary>
	/// <param name="cookiesLocation">The full file name of the file cookies should be stored in.</param>
	/// <param name="cancellationToken">A standard cancellation token that cancels any long operation.</param>
	public SimpleClient(string cookiesLocation, CancellationToken cancellationToken)
		: this(null, cookiesLocation, null, cancellationToken)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="SimpleClient"/> class.</summary>
	/// <param name="contactInfo">Contact info to be sent in user agent.</param>
	/// <param name="cookiesLocation">The full file name of the file cookies should be stored in. If <see langword="null"/>, cookies will not be persisted between sessions.</param>
	/// <param name="credentials">Authentication for <see cref="HttpClientHandler"/> if site requires a password in order to reach the wiki.</param>
	/// <param name="cancellationToken">A standard cancellation token that cancels any long operation.</param>
	public SimpleClient(string? contactInfo, string? cookiesLocation, ICredentials? credentials, CancellationToken cancellationToken)
	{
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

		HttpClient client = new(this.retryHandler)
		{
			Timeout = TimeSpan.FromSeconds(300) // TODO: Manually added timeout for now. Excessive to allow large downloads like ESO icons file. Better to see if we can chunk the download or something.
		};

		var headers = client.DefaultRequestHeaders;
		headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
		headers.ExpectContinue = false;
		headers.UserAgent.ParseAdd(this.UserAgent);
		this.httpClient = client;
	}
	#endregion

	#region Events

	/// <inheritdoc/>
	public event StrongEventHandler<IMediaWikiClient, DelayEventArgs>? DelayComplete;

	/// <inheritdoc/>
	public event StrongEventHandler<IMediaWikiClient, DelayEventArgs>? RequestingDelay;
	#endregion

	#region Public Properties

	/// <summary>Gets or sets the number of times a request will be retried if it fails.</summary>
	public int Retries { get; set; } = 3;

	/// <summary>Gets or sets the amount of time between retries if a request fails.</summary>
	public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(10);

	private string UserAgent { get; }
	#endregion

	#region Public Methods

	/// <summary>Clears all cookies for a specific URI or for all URIs.</summary>
	/// <param name="uri">The URI whose cookies should be cleared. If <see langword="null"/>, cookies for all URIs will be cleared.</param>
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

	/// <inheritdoc/>
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc/>
	public bool DownloadFile(Uri uri, string? fileName) => this.DownloadFileAsync(uri, fileName).Result;

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

	/// <inheritdoc/>
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

	/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
	/// <param name="disposing"><see langword="true"/> if the object is being disposed; <see langword="false"/> if it's finalizing.</param>
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

	/// <summary>The action to be taken when after a requested delay has been completed.</summary>
	/// <param name="e">The sending <see cref="DelayEventArgs"/>.</param>
	/// <seealso cref="OnRequestingDelay(DelayEventArgs)"/>
	protected virtual void OnDelayComplete(DelayEventArgs e) => this.DelayComplete?.Invoke(this, e);

	/// <summary>The action to be taken when a delay is requested. Delays can be the result of a Retry-After header, an error, or an external process that needs to throttle communications.</summary>
	/// <param name="e">The sending <see cref="DelayEventArgs"/>.</param>
	/// <seealso cref="OnDelayComplete(DelayEventArgs)"/>
	protected virtual void OnRequestingDelay(DelayEventArgs e) => this.RequestingDelay?.Invoke(this, e);
	#endregion

	#region Private Static Methods
	private static string GetResponseText(HttpResponseMessage response)
	{
		using var respStream = response.Content.ReadAsStream();
		using StreamReader reader = new(respStream);
		return reader.ReadToEnd();
	}
	#endregion

	#region Private Methods

	private async Task<bool> DownloadFileAsync(Uri uri, string? fileName)
	{
		ArgumentNullException.ThrowIfNull(uri);

		// TODO: This was cobbled together from internet sources and analyzer suggestions. My async programming is weak, but there seems to me to be a lot of awaits here. Can any of it be optimized?
		using var response = await this.httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, this.cancellationToken).ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			return false;
		}

		var responseStream = await response.Content.ReadAsStreamAsync(this.cancellationToken).ConfigureAwait(false);
		await using (responseStream.ConfigureAwait(false))
		{
			{
				var outStream = fileName is null
					? Stream.Null
					: File.OpenWrite(fileName);
				await using (outStream.ConfigureAwait(false))
				{
					await responseStream.CopyToAsync(outStream, this.cancellationToken).ConfigureAwait(false);
				}
			}
		}

		this.SaveCookies();
		return true;
	}

	private void LoadCookies()
	{
		if (this.cookiesLocation is null)
		{
			return;
		}

		var cookieText = string.Empty;
		try
		{
			cookieText = File.ReadAllText(this.cookiesLocation);
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

		if (cookieText.Length > 0 && JsonConvert.DeserializeObject<CookieCollection>(cookieText, this.jsonSettings) is CookieCollection cookies)
		{
			foreach (var cookie in cookies)
			{
				if (cookie is Cookie realCookie)
				{
					try
					{
						this.cookieContainer.Add(realCookie);
					}
					catch (CookieException)
					{
						Debug.WriteLine("Failed: " + realCookie.ToString());
					}
				}
			}

			this.previousHash = Globals.GetHash(cookieText, CookieHashType);
		}
	}

	/// <summary>Saves all cookies to persistent storage.</summary>
	private void SaveCookies()
	{
		if (this.cookiesLocation is null)
		{
			return;
		}

		var jsonCookies = JsonConvert.SerializeObject(this.cookieContainer.GetAllCookies(), SerializerSettings);
		var hash = Globals.GetHash(jsonCookies, CookieHashType);
		if (!hash.OrdinalEquals(this.previousHash))
		{
			this.previousHash = hash;
			File.WriteAllText(this.cookiesLocation, jsonCookies);
		}
	}
	#endregion
}