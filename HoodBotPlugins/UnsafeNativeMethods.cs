namespace RobinHood70.HoodBotPlugins
{
	using System.Runtime.InteropServices;
	using System.Security;

	[SuppressUnmanagedCodeSecurity]
	internal static class UnsafeNativeMethods
	{
		// Implement the IOleMessageFilter interface.
		[DllImport("ole32.dll", ExactSpelling = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		public static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldMsgFilter);
	}
}
