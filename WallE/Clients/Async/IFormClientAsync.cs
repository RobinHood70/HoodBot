#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (file is not currently being maintained)
namespace RobinHood70.WallE.Clients.Async
{
	using System;
	using System.Net;
	using System.Net.Http;
	using System.Threading.Tasks;
	using WikiCommon;

	public interface IFormClientAsync
	{
		#region Events
		event StrongEventHandler<IFormClientAsync, DelayEventArgs> RequestingDelay;
		#endregion

		#region Methods
		Task<string> GetAsync(Uri uri);

		CookieContainer LoadCookies();

		Task<string> PostAsync(Uri uri, HttpContent content);

		/// <summary>This method is used both to throttle clients as well as to respect any wiki-requested delays, such as from maxlag. Clients should respect any delays requested unless they expect to abort the procedure, or for testing.</summary>
		/// <param name="delayTime">The amount of time to delay for.</param>
		/// <param name="reason">The reason for the delay, as specified by the caller.</param>
		/// <returns>A value indicating whether or not the delay was respected.</returns>
		Task<bool> RequestDelayAsync(TimeSpan delayTime, DelayReason reason);

		void SaveCookies();
		#endregion
	}
}
