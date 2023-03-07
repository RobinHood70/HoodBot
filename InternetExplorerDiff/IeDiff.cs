namespace RobinHood70.InternetExplorerDiff
{
	using System;
	using System.ComponentModel.Composition;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBotPlugins;
	using RobinHood70.InternetExplorerDiff.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using SHDocVw;

	[Export(typeof(IPlugin))]
	[ExportMetadata("DisplayName", "Internet Explorer")]
	public class IeDiff : IDiffViewer
	{
		#region Private Constants
		private const int ComSleep = 1000;
		#endregion

		#region Fields
		private Process? ieProcess;
		#endregion

		#region Public Properties
		public string Name => Resources.Name;
		#endregion

		#region Public Methods
		[STAThread]
		public void Compare(DiffContent diff)
		{
			diff.ThrowNull();
			diff.EditPath.PropertyThrowNull(nameof(IeDiff), nameof(diff.EditPath));
			const int empty = 0;
			const string headers = "Content-Type: application/x-www-form-urlencoded";

			InternetExplorer? ie = null;
			for (var i = 0; i < 10; i++)
			{
				try
				{
					ie = new InternetExplorer();
					break;
				}
				catch (COMException) when (i < 10)
				{
					Thread.Sleep(ComSleep);
				}
			}

			if (ie == null)
			{
				return;
			}

			var disposable = new object();
#pragma warning disable CA2020 // Prevent from behavioral change
			HandleRef hwnd = new(disposable, (IntPtr)ie.HWND);
#pragma warning restore CA2020 // Prevent from behavioral change
			_ = SafeNativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
			var processId32 = Convert.ToInt32(processId);
			if (processId32 == 0)
			{
				throw new InvalidOperationException();
			}

			this.ieProcess = Process.GetProcessById(processId32);

			Uri uri = new(diff.EditPath.ToString().Replace("action=edit", "action=submit", StringComparison.Ordinal));
			Request request = new(uri, RequestType.Post, false);
			request
				.Add("wpDiff", "Show changes")
				.Add("wpTextbox1", diff.Text)
				.AddIf("wpMinoredit", "1", diff.IsMinor)
				.Add("wpSummary", diff.EditSummary)
				.Add("wpAutoSummary", string.Empty.GetHash(HashType.Md5))
				.Add("wpEditToken", diff.EditToken)
				.Add("wpStarttime", IndexDateTime(diff.StartTimestamp))
				.Add("wpEdittime", IndexDateTime(diff.LastRevisionTimestamp ?? diff.StartTimestamp ?? DateTime.Now));
			var postData = RequestVisitorUrl.Build(request);
			var byteData = Encoding.UTF8.GetBytes(postData);
			var error = true;
			do
			{
				try
				{
					ie.Navigate(request.Uri.AbsoluteUri, empty, empty, byteData, headers);
					error = false;
				}
				catch (COMException)
				{
					Thread.Sleep(ComSleep);
				}
			}
			while (error);

			SafeNativeMethods.ShowWindow(hwnd, 3);
		}

		public bool Validate() => OperatingSystem.IsWindows() && Type.GetTypeFromProgID("InternetExplorer.Application") != null;

		public void Wait() => this.ieProcess?.WaitForExit();
		#endregion

		#region Private Methods
		[return: NotNullIfNotNull(nameof(dt))]
		private static string? IndexDateTime(DateTime? dt) => dt?.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		#endregion
	}
}