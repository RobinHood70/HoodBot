namespace RobinHood70.HoodBot.DiffViewers
{
	using System.Runtime.InteropServices;

	// This code taken from https://docs.microsoft.com/en-us/previous-versions/ms228772(v=vs.140). It is intended to handle ComException that arise when trying to automate Visual Studio.
	// Class containing the IOleMessageFilter thread error-handling functions.
	internal class MessageFilter : IOleMessageFilter
	{
		internal const int ServerCallIsHandled = 0;
		internal const int ServerCallRetryLater = 2;
		internal const int PendingMessageWaitDeferProcess = 2;

#pragma warning disable CA1806 // Do not ignore method results

		// Start the filter.
		public static void Register()
		{
			IOleMessageFilter newFilter = new MessageFilter();
			CoRegisterMessageFilter(newFilter, out _);
		}

		// Done with the filter, close it.
		public static void Revoke() => CoRegisterMessageFilter(null, out _);
#pragma warning restore CA1806 // Do not ignore method results

		// IOleMessageFilter functions.
		// Handle incoming thread requests.
		int IOleMessageFilter.HandleInComingCall(int dwCallType, System.IntPtr hTaskCaller, int dwTickCount, System.IntPtr lpInterfaceInfo) => ServerCallIsHandled;

		// Thread call was rejected, so try again.
		// RetryLater = Retry the thread call immediately if return >=0 & < 100.
		// Otherwise: Too busy; cancel call.
		int IOleMessageFilter.RetryRejectedCall(System.IntPtr hTaskCallee, int dwTickCount, int dwRejectType) => dwRejectType == ServerCallRetryLater ? 99 : -1;

		int IOleMessageFilter.MessagePending(System.IntPtr hTaskCallee, int dwTickCount, int dwPendingType) => PendingMessageWaitDeferProcess;

		// Implement the IOleMessageFilter interface.
		[DllImport("Ole32.dll")]
		private static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldFilter);
	}
}