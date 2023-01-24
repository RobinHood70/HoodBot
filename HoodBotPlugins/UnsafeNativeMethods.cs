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
#pragma warning disable SYSLIB1054 // Not a simple conversion; no examples on Internet yet. Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
		public static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldMsgFilter);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
	}
}
