namespace RobinHood70.HoodBotPlugins
{
	using System;
	using System.Runtime.InteropServices;

	/// <summary>Provides COM servers and applications with the ability to selectively handle incoming and outgoing COM messages while waiting for responses from synchronous calls. Filtering messages helps to ensure that calls are handled in a manner that improves performance and avoids deadlocks. COM messages can be synchronous, asynchronous, or input-synchronized; the majority of interface calls are synchronous.</summary>
	/// <remarks>See <see href="https://docs.microsoft.com/en-us/windows/desktop/api/objidl/nn-objidl-imessagefilter"/> for more information.</remarks>
	[ComImport]
	[Guid("00000016-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IOleMessageFilter
	{
		/// <summary>Provides a single entry point for incoming calls.</summary>
		/// <param name="dwCallType">The type of incoming call that has been received. Possible values are from the enumeration CALLTYPE.</param>
		/// <param name="hTaskCaller">The thread id of the caller.</param>
		/// <param name="dwTickCount">The elapsed tick count since the outgoing call was made, if dwCallType is not CALLTYPE_TOPLEVEL. If dwCallType is CALLTYPE_TOPLEVEL, dwTickCount should be ignored.</param>
		/// <param name="lpInterfaceInfo">A pointer to an INTERFACEINFO structure that identifies the object, interface, and method being called. In the case of DDE calls, lpInterfaceInfo can be NULL because the DDE layer does not return interface information.</param>
		/// <returns>A SERVICECALL result indicating status.</returns>
		/// <remarks>See <see href="https://docs.microsoft.com/en-us/windows/desktop/api/objidl/nf-objidl-imessagefilter-handleincomingcall"/> for more information.</remarks>
		[PreserveSig]
		int HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);

		/// <summary>Indicates that a message has arrived while COM is waiting to respond to a remote call.</summary>
		/// <param name="hTaskCallee">The thread id of the called application.</param>
		/// <param name="dwTickCount">The number of ticks since the call was made. It is calculated from the GetTickCount function.</param>
		/// <param name="dwPendingType">The type of call made during which a message or event was received. Possible values are from the enumeration PENDINGTYPE, where PENDINGTYPE_TOPLEVEL means the outgoing call was not nested within a call from another application and PENDINTGYPE_NESTED means the outgoing call was nested within a call from another application.</param>
		/// <returns>A PENDINGMSG result indicating status.</returns>
		/// <remarks>See <see href="https://docs.microsoft.com/en-us/windows/desktop/api/objidl/nf-objidl-imessagefilter-messagepending"/> for more information.</remarks>
		[PreserveSig]
		int MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType);

		/// <summary>Provides applications with an opportunity to display a dialog box offering retry, cancel, or task-switching options.</summary>
		/// <param name="hTaskCallee">The thread id of the called application.</param>
		/// <param name="dwTickCount">The number of elapsed ticks since the call was made.</param>
		/// <param name="dwRejectType">Specifies either SERVERCALL_REJECTED or SERVERCALL_RETRYLATER, as returned by the object application.</param>
		/// <returns>A value indicating how the call should proceed.</returns>
		/// <remarks>See <see href="https://docs.microsoft.com/en-us/windows/desktop/api/objidl/nf-objidl-imessagefilter-retryrejectedcall"/> for more information.</remarks>
		[PreserveSig]
		int RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType);
	}
}