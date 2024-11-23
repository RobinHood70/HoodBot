#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (file is not currently being maintained)
namespace RobinHood70.WallE.Clients;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;

// This class adds retry functionality to the standard HttpClient.
// It could also have derived from WebRequestHandler directly, but it's hinted by MS and others that this is the better way to do things in case people want to add further handlers to the chain.
public class SimpleClientRetryHandler : DelegatingHandler
{
	#region Fields
	private readonly SimpleClient parent;
	#endregion

	#region Constructors
	internal SimpleClientRetryHandler(SimpleClient parent, HttpMessageHandler innerHandler)
		: base(innerHandler)
	{
		this.parent = parent;
	}
	#endregion

	#region Protected Override Methods
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Number of missing cases is prohibitively long.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0072:Add missing cases", Justification = "Number of missing cases is prohibitively long.")]
	protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		HttpResponseMessage retval;
		try
		{
			retval = base.Send(request, cancellationToken);
		}
		catch (HttpRequestException hre) when (hre.InnerException is SocketException se)
		{
			var responseCode = se.SocketErrorCode switch
			{
				SocketError.ConnectionRefused => HttpStatusCode.Forbidden,
				SocketError.HostNotFound => HttpStatusCode.NotFound,
				SocketError.TimedOut => HttpStatusCode.RequestTimeout,
				_ => HttpStatusCode.BadRequest
			};

			return new HttpResponseMessage(responseCode);
		}

		var retry = this.parent.Retries;
		while (retry > 0)
		{
			// Checks status for non-standard success as well as the more common unrecoverable errors and returns immediately if we've got one of those. Falls through on OK so we can check for maxlag information.
			switch (retval.StatusCode)
			{
				case HttpStatusCode.OK:
					break;
				case HttpStatusCode.MovedPermanently: // Not sure how we're getting this, with auto-redirection enabled, but we are.
				case HttpStatusCode.BadRequest:
				case HttpStatusCode.Forbidden:
				case HttpStatusCode.InternalServerError:
				case HttpStatusCode.NotFound:
				case HttpStatusCode.RequestUriTooLong:
				case HttpStatusCode.ServiceUnavailable: // Theoretically we could get a response, but if it's too busy or temporarily down, stop bugging it.
				case HttpStatusCode.Unauthorized:
				case > HttpStatusCode.OK and < HttpStatusCode.Ambiguous:
					return retval;
				default:
					break;
			}

			TimeSpan retryAfter;
			DelayReason reason;
			if (retval.Headers.RetryAfter is RetryConditionHeaderValue retryHeader)
			{
				retryAfter = retryHeader.Delta ?? this.parent.RetryDelay;
				reason = DelayReason.MaxLag;
			}
			else
			{
				if (retval.StatusCode == HttpStatusCode.OK)
				{
					return retval;
				}

				// If we didn't get OK or a maxlag retry, then this is a legitimate error. Decrement retry count and use general retry delay.
				retry--;
				retryAfter = this.parent.RetryDelay;
				reason = DelayReason.Error;
			}

			this.parent.RequestDelay(retryAfter, reason, retval.ReasonPhrase ?? "Unspecified Delay Request");

			retval = base.Send(request, cancellationToken);
		}

		return retval;
	}
	#endregion
}