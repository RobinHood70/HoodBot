namespace RobinHood70.HoodBot.DiffViewers
{
	using System.Runtime.InteropServices;

	internal class MessageFilter : IOleMessageFilter
	{
		// Class containing the IOleMessageFilter thread error-handling functions.

		// Start the filter.
		public static void Register()
		{
			IOleMessageFilter newFilter = new MessageFilter();
#pragma warning disable CA1806 // Do not ignore method results
			CoRegisterMessageFilter(newFilter, out var _);
#pragma warning restore CA1806 // Do not ignore method results
		}

		// Done with the filter, close it.
#pragma warning disable CA1806 // Do not ignore method results
		public static void Revoke() => CoRegisterMessageFilter(null, out var _);
#pragma warning restore CA1806 // Do not ignore method results

		// IOleMessageFilter functions.
		// Handle incoming thread requests.
		int IOleMessageFilter.HandleInComingCall(int dwCallType, System.IntPtr hTaskCaller, int dwTickCount, System.IntPtr lpInterfaceInfo) => 0; // Return the flag SERVERCALL_ISHANDLED.

		// Thread call was rejected, so try again.
		int IOleMessageFilter.RetryRejectedCall(System.IntPtr hTaskCallee, int dwTickCount, int dwRejectType)
		{
			// flag = SERVERCALL_RETRYLATER.
			if (dwRejectType == 2)
			{
				// Retry the thread call immediately if return >=0 & < 100.
				return 99;
			}

			// Too busy; cancel call.
			return -1;
		}

		int IOleMessageFilter.MessagePending(System.IntPtr hTaskCallee, int dwTickCount, int dwPendingType) => 2; // Return the flag PENDINGMSG_WAITDEFPROCESS.

		// Implement the IOleMessageFilter interface.
		[DllImport("Ole32.dll")]
		private static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldFilter);
	}
}