namespace WallE.Http
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.IO;
	using System.Net;
	using System.Net.Cache;
	using System.Text;
	using WallE.Properties;

	public class OldClient : ClientBase
	{
		#region Fields
		private string contentType;
		private long contentLength = 0;
		private int retryAfter;
		#endregion

		#region Constructors
		public OldClient()
			: this(null)
		{
		}

		public OldClient(string userAgent)
			: base(userAgent)
		{
			this.Cookies = new CookieContainer();
		}
		#endregion

		#region Protected Properties
		protected CookieContainer Cookies { get; set; }

		protected string RequestData { get; set; }

		protected Uri RequestUri { get; set; }
		#endregion

		#region Public Override Methods
		[SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Rule presumes bad Dispose design")]
		public override string GetResult(PhpRequest request)
		{
			string retval = null;
			var errorCounter = this.ErrorRetryCount;
			this.TransformRequest(request);

			do
			{
				this.OnGettingResult(new GetResultEventArgs(request));
				try
				{
					using (var response = this.GetWebResponse())
					using (var stream = response.GetResponseStream())
					using (var streamReader = new StreamReader(stream))
					{
						retval = streamReader.ReadToEnd();
					}

					if (this.retryAfter == 0)
					{
						errorCounter = 0;
					}
					else
					{
						this.RequestDelay(new TimeSpan(0, 0, 0, this.retryAfter, 500), "maxlag");
					}
				}
				catch (WebException we)
				{
					errorCounter--;
					if (errorCounter == 0)
					{
						throw new ClientException(Messages.WebReqFailed, we);
					}

					this.RequestDelay(this.ErrorRetryDelay, "Web exception: " + we.Message);
				}
			}
			while (errorCounter > 0);

			return retval;
		}
		#endregion

		#region Protected Virtual Methods
		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "More expensive than a simple property.")]
		protected virtual WebRequest GetWebRequest()
		{
			var request = (HttpWebRequest)WebRequest.Create(this.RequestUri);
			request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
			request.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.Revalidate);
			if (this.contentLength > 0)
			{
				request.ContentLength = this.contentLength;
			}

			request.ContentType = this.contentType;
			request.CookieContainer = this.Cookies;
			request.ReadWriteTimeout = 60000; // More reasonable read-write timeout than the default of 5 minutes
			if (this.RequestData == null)
			{
				request.Method = "GET";
			}
			else
			{
				request.Method = "POST";
				var postData = Encoding.UTF8.GetBytes(this.RequestData);
				using (var stream = request.GetRequestStream())
				{
					stream.Write(postData, 0, postData.Length);
				}
			}

			if (this.UserAgent != null)
			{
				request.Headers[HttpRequestHeader.UserAgent] = this.UserAgent;
			}

			return request;
		}

		[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "Method accesses web.")]
		protected virtual WebResponse GetWebResponse()
		{
			var request = this.GetWebRequest();

			// If a RetryAfter header is found, it must be from maxlag (because there's nothing else in MW that emits that), so save that value and continue. Re-throw error otherwise so that this function behaves as it normally would.
			//
			// Note that API maxlag returns an OK response, while Index maxlag returns a 503 Service Unavailable error, so we have to check both.
			HttpWebResponse retval;
			try
			{
				retval = (HttpWebResponse)request.GetResponse();
				this.retryAfter = Convert.ToInt32(retval.Headers[HttpResponseHeader.RetryAfter], CultureInfo.InvariantCulture);
			}
			catch (WebException we)
			{
				retval = (HttpWebResponse)we.Response;
				if (retval == null)
				{
					throw;
				}

				this.retryAfter = Convert.ToInt32(retval.Headers[HttpResponseHeader.RetryAfter], CultureInfo.InvariantCulture);
				if (this.retryAfter == 0)
				{
					throw;
				}
			}

			return retval;
		}

		protected virtual void TransformRequest(PhpRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request), Globals.InputNull(nameof(OldClient), nameof(this.TransformRequest)));
			}

			this.contentLength = 0;
			switch (request.Type)
			{
				default:
				case RequestType.Get:
					this.RequestUri = request.FullUri;
					this.RequestData = null;
					this.contentType = "application/x-www-form-urlencoded";
					break;
				case RequestType.Post:
					this.RequestUri = request.Uri;
					this.RequestData = request.UrlEncodedData;
					this.contentType = "application/x-www-form-urlencoded";
					break;
				case RequestType.PostMultipart:
					// TODO: Check with international and byte data.
					this.RequestUri = request.Uri;
					var multipartData = request.MultipartData;
					this.RequestData = multipartData.ReadAsStringAsync().Result;
					foreach (var header in multipartData.Headers)
					{
						// Is it safe to assume we always get only one value? RFC2616 allows for multiples, but I can't see actually getting multiples here.
						var valueEnumerator = header.Value.GetEnumerator();
						valueEnumerator.MoveNext();
						var value = valueEnumerator.Current;

						switch (header.Key)
						{
							case "Content-Type":
								this.contentType = value;
								break;
							case "Content-Length":
								this.contentLength = long.Parse(value, CultureInfo.InvariantCulture);
								break;
						}
					}

					break;
			}
		}
		#endregion
	}
}