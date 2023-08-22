namespace RobinHood70.InternetExplorerDiff
{
	using System.Runtime.InteropServices;
	using System.Security;

	[SuppressUnmanagedCodeSecurity]
	internal static class SafeNativeMethods
	{
		[DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto/*, SetLastError = true*/)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		public static extern uint GetWindowThreadProcessId(HandleRef hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShowWindow(HandleRef hWnd, int nCmdShow);
	}
}