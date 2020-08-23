namespace RobinHood70.WallE.Clients
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.IO.Compression;
	using System.Net;
	using System.Runtime.Serialization;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Security;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Design;
	using RobinHood70.WallE.Properties;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.Clients.ClientShared;

	// TODO: Add cancellation token possibilities so requests can be cancelled if they're failing without waiting for all retries.

	/// <summary>This class provides basic HTTP and cookie handling, with MediaWiki maxlag support.</summary>
	/// <seealso cref="IMediaWikiClient" />
	public class SimpleClient : IMediaWikiClient
	{
		#region Fields
		private readonly string cookiesLocation;
		private CookieContainer cookieContainer;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="SimpleClient" /> class.</summary>
		public SimpleClient()
			: this(null, null)
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
		public SimpleClient(string? contactInfo, string? cookiesLocation)
		{
			// Test for the Mono Z-Stream bug on Windows: http://stackoverflow.com/a/32958861/502255
			if (HasMono && OnWindows && (DefaultAcceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase) || DefaultAcceptEncoding.Contains("deflate", StringComparison.OrdinalIgnoreCase)))
			{
				try
				{
					var bytes = new byte[] { 0x1f, 0x8b, 0x08, 0, 0, 0, 0, 0, 4, 0, 0x63, 0, 0, 0x8d, 0xef, 2, 0xd2, 1, 0, 0, 0 };
					using var compressedStream = new MemoryStream(bytes);
					using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
					using var resultStream = new MemoryStream();
					zipStream.CopyTo(resultStream);
					resultStream.ToArray(); // We don't actually need the result, we just want to be sure the stream has been checked.
				}
				catch (EntryPointNotFoundException)
				{
					throw new InvalidOperationException(Messages.MonoCreateZStreamBug);
				}
				catch (DllNotFoundException)
				{
					throw new InvalidOperationException(Messages.MonoCreateZStreamBug);
				}
			}

			ServicePointManager.Expect100Continue = false;
			this.UserAgent = BuildUserAgent(contactInfo);
			this.cookiesLocation = cookiesLocation ?? DefaultCookiesLocation;
			this.cookieContainer = this.GetCookieContainer();
		}
		#endregion

		#region Events

		/// <summary>The event raised when either the site or the client is requesting a delay.</summary>
		public event StrongEventHandler<IMediaWikiClient, DelayEventArgs>? RequestingDelay;
		#endregion

		#region Public Static Properties

		/// <summary>Gets or sets the default encoding types. There is normally no need to change this value; it is provided to overcome an old Mono bug, and for the rare case that the default encodings are unavailable or new encodings are.</summary>
		/// <value>The default AcceptEncoding value.</value>
		public static string DefaultAcceptEncoding { get; set; } = "gzip, deflate";

		/// <summary>Gets or sets the default location to store cookies. As a static variable, this will apply across all new instances of the client. Existing clients will continue to use the whatever was provided or was the default at their instantiation.</summary>
		/// <value>The default cookies location.</value>
		public static string DefaultCookiesLocation { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cookies.dat");

		/// <summary>Gets a value indicating whether the current project is using <see href="http://www.mono-project.com/">Mono</see>.</summary>
		/// <value><see langword="true"/> if this instance is running on Mono; otherwise, <see langword="false"/>.</value>
		public static bool HasMono { get; } = Type.GetType("Mono.Runtime") != null;

		/// <summary>Gets a value indicating whether the current project is running on Windows.</summary>
		/// <value><see langword="true"/> if running on Windows; otherwise, <see langword="false"/>.</value>
		public static bool OnWindows { get; } = Environment.OSVersion.Platform < PlatformID.Unix;

		/// <summary>Gets the amount of time (in seconds) to add to retries if they occur in succession.</summary>
		/// <value>The retry delay bonuses.</value>
		/// <remarks>Although default values are provided, these are user-settable. In the event that more retries are allowed than there are entries in this list, the last retry delay bonus will be used.</remarks>
		public static IList<int> RetryDelayBonuses { get; } = new List<int>()
		{
			0, 0, 0, 0, 0,
			5, 10, 15, 20, 25,
			30, 30, 30, 60, 60,
			120, 120, 300, 1800, 3600
		};
		#endregion

		#region Public Properties

		/// <summary>Gets or sets a name for this instance of the client. It has no effect on operation and is provided primarily as a debugging tool to differentiate between multiple client instances.</summary>
		/// <value>The instance name.</value>
		public string Name { get; set; } = nameof(SimpleClient);

		/// <summary>Gets or sets the number of times to retry an operation in the event of a temporary failure such as a connection loss.</summary>
		/// <value>The number of retries.</value>
		public int Retries { get; set; } = 20;

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
		/// <returns><see langword="true"/> if the download succeeded; otherwise <see langword="false"/>.</returns>
		/// <exception cref="WikiException">HTTP request failed.</exception>
		public bool DownloadFile(Uri uri, string fileName)
		{
			ThrowNull(uri, nameof(uri));
			ThrowNull(fileName, nameof(fileName));
			using (var response = this.SendRequest(uri, "GET", null, null, false))
			{
				if (GetResponseData(response) is byte[] retval)
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
			}

			return false;
		}

		/// <summary>Gets the text of the result returned by the given URI.</summary>
		/// <param name="uri">The URI to get.</param>
		/// <returns>The text of the result.</returns>
		public string Get(Uri uri)
		{
			using var response = this.SendRequest(uri, "GET", null, null, true);
			return GetResponseText(response);
		}

		/// <summary>Retrieves cookies from persistent storage.</summary>
		public void LoadCookies() => this.cookieContainer = this.GetCookieContainer();

		/// <summary>POSTs text data and retrieves the result.</summary>
		/// <param name="uri">The URI to POST data to.</param>
		/// <param name="postData">The text to POST.</param>
		/// <returns>The text of the result.</returns>
		public string Post(Uri uri, string postData)
		{
			using var response = this.SendRequest(uri, "POST", FormUrlEncoded, Encoding.UTF8.GetBytes(postData), true);
			return GetResponseText(response);
		}

		/// <summary>POSTs text data and retrieves the result.</summary>
		/// <param name="uri">The URI to POST data to.</param>
		/// <param name="contentType">The text of the content type. Typicially <c>x-www-form-urlencoded</c> or <c>multipart/form-data (...)</c>, but there is no restriction on values.</param>
		/// <param name="postData">The text to POST.</param>
		/// <returns>The text of the result.</returns>
		public string Post(Uri uri, string contentType, byte[] postData)
		{
			using var response = this.SendRequest(uri, "POST", contentType, postData, true);
			return GetResponseText(response);
		}

		/// <summary>This method is used both to throttle clients as well as to forward any wiki-requested delays, such as from maxlag. Clients should respect any delays requested by the wiki unless they expect to abort the procedure, or for testing.</summary>
		/// <param name="delayTime">The amount of time to delay for.</param>
		/// <param name="reason">The reason for the delay, as specified by the caller.</param>
		/// <param name="description">The human-readable reason for the delay, as specified by the caller.</param>
		/// <returns>A value indicating whether or not the delay was respected.</returns>
		public bool RequestDelay(TimeSpan delayTime, DelayReason reason, string description)
		{
			if (delayTime <= TimeSpan.Zero)
			{
				return true;
			}

			var e = new DelayEventArgs(delayTime, reason, description);
			this.OnRequestingDelay(e);
			if (e.Cancel)
			{
				return false;
			}

			// Thread.Sleep(delayTime);

			// Temporary workaround for Thread.Sleep locking the UI thread.
			// TODO: Make this work with pause/cancel tokens. Right now, this bypasses that process completely.
			Task.Run(() => Thread.Sleep(delayTime)).Wait();

			return true;
		}

		/// <summary>Saves all cookies to persistent storage.</summary>
		public void SaveCookies()
		{
			if (this.cookiesLocation != null)
			{
				using var stream = File.Create(this.cookiesLocation);
				new BinaryFormatter().Serialize(stream, this.cookieContainer);
			}
		}
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
		/// <returns>A <see cref="string" /> that represents this instance.</returns>
		public override string ToString() => this.Name;
		#endregion

		#region Protected Virtual Methods

		/// <summary>Raises the <see cref="RequestingDelay" /> event.</summary>
		/// <param name="e">The <see cref="DelayEventArgs" /> instance containing the event data.</param>
		protected virtual void OnRequestingDelay(DelayEventArgs e) => this.RequestingDelay?.Invoke(this, e);
		#endregion

		#region Private Static Methods
		private static byte[]? GetResponseData(HttpWebResponse response)
		{
			if (response != null)
			{
				using var respStream = response.GetResponseStream();
				using var mem = new MemoryStream();
				respStream.CopyTo(mem);
				return mem.ToArray();
			}

			return null;
		}

		private static string GetResponseText(HttpWebResponse response)
		{
			ThrowNull(response, nameof(response));
			using var respStream = response.GetResponseStream();
			using var reader = new StreamReader(respStream);
			return reader.ReadToEnd();
		}

		/*
		private static bool PerSessionUnsafeHeaderParsing(Exception ex)
		{
			if (!ex.Message.Contains("Section=ResponseStatusLine"))
			{
				return false;
			}

			// Adapted from http://stackoverflow.com/a/8523437/502255
			var assembly = Assembly.GetAssembly(typeof(SettingsSection));
			var type = assembly?.GetType("System.Net.Configuration.SettingsSectionInternal");
			var section = type?.InvokeMember("Section", BindingFlags.Static | BindingFlags.GetProperty | BindingFlags.NonPublic, null, null, Array.Empty<object>(), CultureInfo.InvariantCulture);
			var useUnsafeHeaderParsing = type?.GetField("useUnsafeHeaderParsing", BindingFlags.NonPublic | BindingFlags.Instance);
			if (section != null && useUnsafeHeaderParsing != null)
			{
				useUnsafeHeaderParsing.SetValue(section, true);
				return true;
			}

			return false;
		}
		*/
		#endregion

		#region Private Methods
		private bool CheckDelay(HttpWebResponse response, int attemptNumber)
		{
			if (response == null)
			{
				return false;
			}

			var retryAfter = TimeSpan.Zero;
			var retryHeader = response.Headers[HttpResponseHeader.RetryAfter];
			if (retryHeader == null)
			{
				// If we didn't get a retry header, check if the response status code indicates a retriable error. If so, use the client's RetryDelay value.
				switch (response.StatusCode)
				{
					case HttpStatusCode.RequestTimeout:
					case HttpStatusCode.BadGateway:
					case HttpStatusCode.GatewayTimeout:
					case (HttpStatusCode)509:
					case HttpStatusCode.ServiceUnavailable:
						retryAfter = this.RetryDelay;
						break;
					default:
						break;
				}
			}
			else
			{
				// Regardless of status code, if we got a retry header, retry after that amount of time.
				retryAfter = int.TryParse(retryHeader, out var retrySeconds) ? TimeSpan.FromSeconds(retrySeconds) : this.RetryDelay;
			}

			if (retryAfter != TimeSpan.Zero)
			{
				if (attemptNumber >= RetryDelayBonuses.Count)
				{
					attemptNumber = RetryDelayBonuses.Count - 1;
				}

				retryAfter += TimeSpan.FromSeconds(RetryDelayBonuses[attemptNumber]);
				var maxlag = response.Headers["X-Database-Lag"];
				var reason = maxlag == null ? DelayReason.Error : DelayReason.MaxLag;
				var description = maxlag == null ? response.StatusDescription : "Database lag: " + maxlag + " seconds.";
				return this.RequestDelay(retryAfter, reason, description);
			}

			return false;
		}

		private HttpWebRequest CreateRequest(Uri uri, string method)
		{
			var request = (HttpWebRequest)WebRequest.Create(uri);
			request.AllowAutoRedirect = true;
			request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
			request.CookieContainer = this.cookieContainer;
			request.Headers[HttpRequestHeader.AcceptEncoding] = "gzip,deflate";
			request.Method = method;
			request.Proxy.Credentials = CredentialCache.DefaultCredentials;
			request.UseDefaultCredentials = true;
			request.UserAgent = this.UserAgent;

			return request;
		}

		private CookieContainer GetCookieContainer()
		{
			// CookieContainer does not play well with serializers other than the BinaryFormatter (and reportedly SoapFormatter). Do not convert this to anything else without testing.
			// CONSIDER: Converting this to JSON serialization (subject to the warning above). Might be doable by simply iterating the cookies in the container rather than trying to de/serialize the entire container.
			if (this.cookiesLocation != null)
			{
				try
				{
					using var stream = File.Open(this.cookiesLocation, FileMode.Open);
					var formatter = new BinaryFormatter() { Binder = new CookieBinder() };
					return formatter.Deserialize(stream) as CookieContainer ?? new CookieContainer();
				}
				catch (DirectoryNotFoundException)
				{
				}
				catch (FileNotFoundException)
				{
				}
			}

			return new CookieContainer();
		}

		private HttpWebResponse SendRequest(Uri uri, string method, string? contentType, byte[]? postData, bool checkMaxLag)
		{
			HttpWebResponse? response = null;
			for (var attempts = 0; attempts < this.Retries; attempts++)
			{
				// Do not try to optimize this out of the loop. A new request must be created every time or else the response returned will be the same response as the previous loop.
				var request = this.CreateRequest(uri, method);
				if (postData?.Length > 0)
				{
					request.ContentType = contentType;
					request.ContentLength = postData.Length;
					using var stream = request.GetRequestStream();
					stream.Write(postData, 0, postData.Length);
				}

				try
				{
					response = (HttpWebResponse)request.GetResponse();
					if (!checkMaxLag || !this.CheckDelay(response, attempts))
					{
						if (response.Cookies is IEnumerable<Cookie> cookies)
						{
							foreach (var cookie in cookies)
							{
								this.cookieContainer.Add(cookie);
							}
						}

						return response;
					}

					response.Dispose();
					response = null;
				}
				catch (WebException ex)
				{
					response?.Dispose();
					response = null;
					/*
					if (PerSessionUnsafeHeaderParsing(ex))
					{
						this.useV10 = true;
						continue;
					}
					*/

					if (ex.Response is HttpWebResponse errorResponse)
					{
						using (ex.Response)
						{
							if (!this.CheckDelay(errorResponse, attempts))
							{
								// If we didn't get a retry header or a retriable error, then throw the error.
								throw;
							}
						}
					}

					if (attempts == this.Retries - 1)
					{
						throw;
					}
				}
				catch (OperationCanceledException)
				{
					this.RequestDelay(TimeSpan.FromSeconds(5), DelayReason.Error, "Http timeout.");
				}
			}

			throw new WikiException(CurrentCulture(Messages.ExcessiveLag));
		}
		#endregion

		#region Private Classes
		private class CookieBinder : SerializationBinder
		{
			private static readonly HashSet<string> ValidTypes = new HashSet<string>
			{
				"System.Collections.ArrayList",
				"System.Collections.Comparer",
				"System.Collections.Hashtable",
				"System.Collections.SortedList",
				"System.Collections.SortedList+SyncSortedList",
				"System.Collections.SortedList+ValueList",
				"System.Globalization.CompareInfo",
				"System.Net.Cookie",
				"System.Net.CookieCollection",
				"System.Net.CookieContainer",
				"System.Net.CookieVariant",
				"System.Net.PathList",
				"System.Net.PathList+PathListComparer",
				"System.Object",
			};

			public override Type BindToType(string assemblyName, string typeName)
			{
				if (ValidTypes.Contains(typeName))
				{
					var type = Type.GetType(typeName + ", " + assemblyName);
					if (type != null)
					{
						return type;
					}
				}

				throw new InvalidDataException();
			}
		}
		#endregion
	}
}