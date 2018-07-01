namespace RobinHood70.WallE.Clients
{
	using System;
	using System.Globalization;
	using System.IO;
	using System.IO.Compression;
	using System.Net;
	using System.Net.Configuration;
	using System.Reflection;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Security;
	using System.Text;
	using System.Threading;
	using RobinHood70.WallE.Design;
	using RobinHood70.WikiCommon;
	using static System.Net.HttpStatusCode;
	using static RobinHood70.WallE.Clients.ClientShared;
	using static RobinHood70.WallE.Properties.Messages;
	using static RobinHood70.WikiCommon.Globals;

	/// <summary>This class provides basic HTTP and cookie handling, with MediaWiki maxlag support.</summary>
	/// <seealso cref="IMediaWikiClient" />
	public class SimpleClient : IMediaWikiClient
	{
		#region Fields
		private readonly string cookiesLocation;
		private CookieContainer cookieContainer;
		private bool useV10 = false;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SimpleClient" /> class.</summary>
		public SimpleClient()
			: this(null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="SimpleClient" /> class with contact information for the User-Agent string.</summary>
		/// <param name="contactInfo">The contact info to be displayed - typically, an e-mail address or user name on the target wiki.</param>
		public SimpleClient(string contactInfo)
			: this(contactInfo, null)
		{
		}

		/// <summary>Initializes a new instance of the <see cref="SimpleClient" /> class with contact information for the User-Agent string.</summary>
		/// <param name="contactInfo">The contact info to be displayed - typically, an e-mail address or user name on the target wiki.</param>
		/// <param name="cookiesLocation">The location and file name to store cookies in across sessions. If null, the default location specified in <see cref="DefaultCookiesLocation" /> will be used.</param>
		public SimpleClient(string contactInfo, string cookiesLocation)
		{
			// Test for the Mono Z-Stream bug on Windows: http://stackoverflow.com/a/32958861/502255
			if (HasMono && OnWindows && (DefaultAcceptEncoding.Contains("gzip") || DefaultAcceptEncoding.Contains("deflate")))
			{
				try
				{
					var bytes = new byte[] { 0x1f, 0x8b, 0x08, 0, 0, 0, 0, 0, 4, 0, 0x63, 0, 0, 0x8d, 0xef, 2, 0xd2, 1, 0, 0, 0 };
					using (var compressedStream = new MemoryStream(bytes))
					using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
					using (var resultStream = new MemoryStream())
					{
						zipStream.CopyTo(resultStream);
						resultStream.ToArray(); // We don't actually need the result, we just want to be sure the stream has been checked.
					}
				}
				catch (EntryPointNotFoundException)
				{
					throw new InvalidOperationException(MonoCreateZStreamBug);
				}
				catch (DllNotFoundException)
				{
					throw new InvalidOperationException(MonoCreateZStreamBug);
				}
			}

			ServicePointManager.Expect100Continue = false;
			this.UserAgent = BuildUserAgent(contactInfo);
			this.cookiesLocation = cookiesLocation ?? DefaultCookiesLocation;
			this.LoadCookies();
		}
		#endregion

		#region Events

		/// <summary>The event raised when either the site or the client is requesting a delay.</summary>
		public event StrongEventHandler<IMediaWikiClient, DelayEventArgs> RequestingDelay;
		#endregion

		#region Public Static Properties

		/// <summary>Gets or sets the default encoding types. There is normally no need to change this value; it is provided to overcome an old Mono bug, and for the rare case that the default encodings are unavailable or new encodings are.</summary>
		/// <value>The default AcceptEncoding value.</value>
		public static string DefaultAcceptEncoding { get; set; } = "gzip, deflate";

		/// <summary>Gets or sets the default location to store cookies. As a static variable, this will apply across all new instances of the client. Existing clients will continue to use the whatever was provided or was the default at their instantiation.</summary>
		/// <value>The default cookies location.</value>
		public static string DefaultCookiesLocation { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cookies.dat");
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a name for this instance of the client. It has no effect on operation and is provided primarily as a debugging tool to differentiate between multiple client instances.</summary>
		/// <value>The instance name.</value>
		public string Name { get; set; } = nameof(SimpleClient);

		/// <summary>Gets or sets the number of times to retry an operation in the event of a temporary failure such as a connection loss.</summary>
		/// <value>The number of retries.</value>
		public int Retries { get; set; } = 3;

		/// <summary>Gets or sets the amount of time to wait between retries of an operation in the event of a temporary failure such as a connection loss.</summary>
		/// <value>The retry delay.</value>
		public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(10);

		/// <summary>Gets or sets the User-Agent string to be sent with each request. If not specified, or if contact info is specified, a default User-Agent string similar to.</summary>
		/// <value>The user agent.</value>
		public string UserAgent { get; set; }
		#endregion

		#region Public Methods

		/// <summary>Deletes all cookies from persistent storage and clears the cookie cache.</summary>
		public void DeleteCookies()
		{
			if (this.cookiesLocation != null)
			{
				try
				{
					File.Delete(this.cookiesLocation);
				}
				catch (FileNotFoundException)
				{
				}
			}

			this.cookieContainer = new CookieContainer();
		}

		/// <summary>Downloads a file directly to disk instead of returning it as a string.</summary>
		/// <param name="uri">The URI to download from.</param>
		/// <param name="fileName">The filename to save to.</param>
		/// <returns><c>true</c> if the download succeeded; otherwise <c>false</c>.</returns>
		/// <exception cref="WikiException">HTTP request failed.</exception>
		public bool DownloadFile(Uri uri, string fileName)
		{
			ThrowNull(uri, nameof(uri));
			ThrowNull(fileName, nameof(fileName));
			HttpWebResponse response = null;
			int retriesRemaining;
			for (retriesRemaining = this.Retries; retriesRemaining > 0; retriesRemaining--)
			{
				var request = this.CreateRequest(uri, "GET");
				try
				{
					response = request.GetResponse() as HttpWebResponse;
					break;
				}
				catch (WebException ex)
				{
					response = ex.Response as HttpWebResponse;
					if (response == null)
					{
						throw;
					}

					if (PerSessionUnsafeHeaderParsing(ex))
					{
						this.useV10 = true;
						continue;
					}

					switch (response.StatusCode)
					{
						case RequestTimeout:
						case BadGateway:
						case GatewayTimeout:
						case (HttpStatusCode)509:
						case ServiceUnavailable:
							break;
						default:
							throw;
					}

					if (retriesRemaining == 0)
					{
						throw new WikiException("HTTP request failed. " + GetResponseText(response));
					}
				}
			}

			var retval = GetResponseData(response);
			if (retval != null)
			{
				try
				{
					File.WriteAllBytes(fileName, retval);
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
			}

			return false;
		}

		/// <summary>Gets the text of the result returned by the given URI.</summary>
		/// <param name="uri">The URI to get.</param>
		/// <returns>The text of the result.</returns>
		public string Get(Uri uri) => this.SendRequest(uri, "GET", null, null);

		/// <summary>Retrieves cookies from persistent storage.</summary>
		public void LoadCookies()
		{
			// CookieContainer does not play well with serializers other than the BinaryFormatter (and reportedly SoapFormatter). Do not convert this to anything else without testing.
			if (this.cookiesLocation != null)
			{
				try
				{
					using (var stream = File.Open(this.cookiesLocation, FileMode.Open))
					{
						this.cookieContainer = new BinaryFormatter().Deserialize(stream) as CookieContainer;
						return;
					}
				}
				catch (DirectoryNotFoundException)
				{
				}
				catch (FileNotFoundException)
				{
				}
			}

			this.cookieContainer = new CookieContainer();
		}

		/// <summary>POSTs text data and retrieves the result.</summary>
		/// <param name="uri">The URI to POST data to.</param>
		/// <param name="postData">The text to POST.</param>
		/// <returns>The text of the result.</returns>
		public string Post(Uri uri, string postData) => this.SendRequest(uri, "POST", FormUrlEncoded, Encoding.UTF8.GetBytes(postData));

		/// <summary>POSTs text data and retrieves the result.</summary>
		/// <param name="uri">The URI to POST data to.</param>
		/// <param name="contentType">The text of the content type. Typicially <c>x-www-form-urlencoded</c> or <c>multipart/form-data (...)</c>, but there is no restriction on values.</param>
		/// <param name="postData">The text to POST.</param>
		/// <returns>The text of the result.</returns>
		public string Post(Uri uri, string contentType, byte[] postData) => this.SendRequest(uri, "POST", contentType, postData);

		/// <summary>This method is used both to throttle clients as well as to forward any wiki-requested delays, such as from maxlag. Clients should respect any delays requested by the wiki unless they expect to abort the procedure, or for testing.</summary>
		/// <param name="delayTime">The amount of time to delay for.</param>
		/// <param name="reason">The reason for the delay, as specified by the caller.</param>
		/// <returns>A value indicating whether or not the delay was respected.</returns>
		public bool RequestDelay(TimeSpan delayTime, DelayReason reason)
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

			Thread.Sleep(delayTime);
			return true;
		}

		/// <summary>Saves all cookies to persistent storage.</summary>
		public void SaveCookies()
		{
			if (this.cookiesLocation != null)
			{
				using (var stream = File.Create(this.cookiesLocation))
				{
					new BinaryFormatter().Serialize(stream, this.cookieContainer);
				}
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => this.Name;
		#endregion

		#region Protected Virtual Methods

		/// <summary>Raises the <see cref="E:RequestingDelay" /> event.</summary>
		/// <param name="e">The <see cref="DelayEventArgs" /> instance containing the event data.</param>
		protected virtual void OnRequestingDelay(DelayEventArgs e) => this.RequestingDelay?.Invoke(this, e);
		#endregion

		#region Private Static Methods
		private static byte[] GetResponseData(HttpWebResponse response)
		{
			if (response != null)
			{
				using (var respStream = response.GetResponseStream())
				using (var mem = new MemoryStream())
				{
					respStream.CopyTo(mem);
					return mem.ToArray();
				}
			}

			return null;
		}

		private static string GetResponseText(HttpWebResponse response)
		{
			if (response == null)
			{
				return null;
			}

			using (var respStream = response.GetResponseStream())
			using (var reader = new StreamReader(respStream))
			{
				return reader.ReadToEnd();
			}
		}

		private static bool PerSessionUnsafeHeaderParsing(Exception ex)
		{
			if (!ex.Message.Contains("Section=ResponseStatusLine"))
			{
				return false;
			}

			// Adapted from http://stackoverflow.com/a/8523437/502255
			var assembly = Assembly.GetAssembly(typeof(SettingsSection));
			var type = assembly?.GetType("System.Net.Configuration.SettingsSectionInternal");
			var section = type?.InvokeMember("Section", BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, new object[0], CultureInfo.InvariantCulture);
			var useUnsafeHeaderParsing = type?.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
			if (section != null && useUnsafeHeaderParsing != null)
			{
				useUnsafeHeaderParsing.SetValue(section, true);
				return true;
			}

			return false;
		}
		#endregion

		#region Private Methods
		private HttpWebRequest CreateRequest(Uri uri, string method)
		{
			var request = WebRequest.Create(uri) as HttpWebRequest;
			request.AllowAutoRedirect = true;
			request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
			request.CookieContainer = this.cookieContainer;
			request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate";
			request.Method = method;
			request.Proxy.Credentials = CredentialCache.DefaultCredentials;
			request.Timeout = 60000;
			request.UseDefaultCredentials = true;
			request.UserAgent = this.UserAgent;
			if (this.useV10)
			{
				request.KeepAlive = false;
				request.ProtocolVersion = HttpVersion.Version10;
			}

			return request;
		}

		private string SendRequest(Uri uri, string method, string contentType, byte[] postData)
		{
			HttpWebResponse response = null;
			int retriesRemaining;
			for (retriesRemaining = this.Retries; retriesRemaining >= 0; retriesRemaining--)
			{
				var request = this.CreateRequest(uri, method);
				if (postData?.Length > 0)
				{
					request.ContentType = contentType;
					request.ContentLength = postData.Length;
					using (var stream = request.GetRequestStream())
					{
						stream.Write(postData, 0, postData.Length);
					}
				}

				var doMaxLagCheck = false;
				try
				{
					response = request.GetResponse() as HttpWebResponse;
					doMaxLagCheck = true;
				}
				catch (WebException ex)
				{
					response = ex.Response as HttpWebResponse;
					if (response != null)
					{
						if (PerSessionUnsafeHeaderParsing(ex))
						{
							this.useV10 = true;
							continue;
						}

						switch (response.StatusCode)
						{
							case RequestTimeout:
							case BadGateway:
							case GatewayTimeout:
							case (HttpStatusCode)509:
								break;
							case ServiceUnavailable:
								doMaxLagCheck = true;
								break;
							default:
								throw;
						}
					}

					if (retriesRemaining == 0)
					{
						throw;
					}
				}

				var retryHeader = response?.Headers[HttpResponseHeader.RetryAfter];
				var retryAfter = 0;
				if (retryHeader == null)
				{
					if (doMaxLagCheck)
					{
						// We got a response with no retry header, therefore it was a successful response. Stop retrying.
						break;
					}

					retryAfter = (int)this.RetryDelay.TotalSeconds;
				}
				else if (!int.TryParse(retryHeader, out retryAfter))
				{
					retryAfter = (int)this.RetryDelay.TotalSeconds;
				}

				if (retryAfter > 0)
				{
					this.RequestDelay(TimeSpan.FromSeconds(retryAfter), doMaxLagCheck ? DelayReason.MaxLag : DelayReason.Error);
				}
			}

			if (response.Cookies != null)
			{
#pragma warning disable IDE0007 // Use implicit type
				foreach (Cookie cookie in response.Cookies)
#pragma warning restore IDE0007 // Use implicit type
				{
					this.cookieContainer.Add(cookie);
				}
			}

			return GetResponseText(response);
		}
		#endregion
	}
}