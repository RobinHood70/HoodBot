#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (file is not currently being maintained)
namespace RobinHood70.WallE.Clients.Async
{
	using System;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;
	using static System.Net.HttpStatusCode;

	// This class adds retry functionality to the standard HttpClient.
	// It could also have derived from WebRequestHandler directly, but it's hinted by MS and others that this is the better way to do things in case people want to add further handlers to the chain.
	public class SimpleClientAsyncRetryHandler : DelegatingHandler
	{
		#region Fields
		private readonly SimpleClientAsync parent;
		#endregion

		#region Constructors
		internal SimpleClientAsyncRetryHandler(SimpleClientAsync parent, HttpMessageHandler innerHandler)
			: base(innerHandler) => this.parent = parent;
		#endregion

		#region Protected Override Methods
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			HttpResponseMessage retval = null;
			for (var i = 0; i < this.parent.Retries; i++)
			{
				retval = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

				// Checks status for the more common unrecoverable errors, or a successful result, and returns immediately if we've got one of those.
				switch (retval.StatusCode)
				{
					case BadRequest:
					case Forbidden:
					case InternalServerError:
					case NotFound:
					case RequestUriTooLong:
					case Unauthorized:
						return retval;
					default:
						if (retval.IsSuccessStatusCode)
						{
							return retval;
						}

						break;
				}

				TimeSpan retryAfter;
				DelayReason reason;
				var retryHeader = retval.Headers.RetryAfter;
				if (retryHeader == null)
				{
					// If we didn't get a maxlag retry, then this is a legitimate error, so use that delay instead.
					retryAfter = this.parent.RetryDelay;
					reason = DelayReason.Error;
				}
				else
				{
					retryAfter = retryHeader.Delta.Value;
					reason = DelayReason.MaxLag;
				}

				await this.parent.RequestDelayAsync(retryAfter, reason, retval.ReasonPhrase).ConfigureAwait(false);
			}

			return retval;
		}
		#endregion
	}
}
