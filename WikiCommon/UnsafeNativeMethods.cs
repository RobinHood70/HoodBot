namespace RobinHood70.WikiCommon
{
	using System.Runtime.InteropServices;
	using System.Security;
	using RobinHood70.WikiCommon.DiffViewers;

	[SuppressUnmanagedCodeSecurity]
	internal static class UnsafeNativeMethods
	{
		// Implement the IOleMessageFilter interface.
		[DllImport("ole32.dll", ExactSpelling = true)]
		public static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldMsgFilter);
	}
}
