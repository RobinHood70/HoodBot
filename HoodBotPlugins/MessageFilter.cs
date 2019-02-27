namespace RobinHood70.HoodBotPlugins
{
	using System;
	using static UnsafeNativeMethods;

	/// <summary>Handles ComException issues that arise when trying to automate some software, like Visual Studio and Internet Explorer.</summary>
	/// <remarks>This code taken from <see href="https://docs.microsoft.com/en-us/previous-versions/ms228772(v=vs.140)"/>.</remarks>
	public sealed class MessageFilter : IOleMessageFilter
	{
		#region Internal Constants
		internal const int ServerCallIsHandled = 0;
		internal const int ServerCallRetryLater = 2;
		internal const int PendingMessageWaitDeferProcess = 2;
		#endregion

		#region Public Static Methods
#pragma warning disable CA1806 // Do not ignore method results
		/// <summary>Starts the filter.</summary>
		public static void Register() => CoRegisterMessageFilter(new MessageFilter(), out _);

		/// <summary>Stops using the filter.</summary>
		public static void Revoke() => CoRegisterMessageFilter(null, out _);
#pragma warning restore CA1806 // Do not ignore method results
		#endregion

		#region Public Methods

		int IOleMessageFilter.HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo) => ServerCallIsHandled;

		int IOleMessageFilter.MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType) => PendingMessageWaitDeferProcess;

		int IOleMessageFilter.RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType) => dwRejectType == ServerCallRetryLater ? 99 : -1;
		#endregion
	}
}