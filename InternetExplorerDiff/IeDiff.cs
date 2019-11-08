namespace RobinHood70.InternetExplorerDiff
{
	using System;
	using System.ComponentModel.Composition;
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading;
	using RobinHood70.HoodBotPlugins;
	using RobinHood70.InternetExplorerDiff.Properties;
	using RobinHood70.WikiCommon.RequestBuilder;
	using SHDocVw;
	using static RobinHood70.WikiCommon.Globals;

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
			ThrowNull(diff, nameof(diff));
			ThrowNull(diff.EditPath, nameof(diff.EditPath));
			InternetExplorer? ie = null;
			for (var i = 0; i < 10; i++)
			{
				try
				{
					ie = new InternetExplorer();
					break;
				}
				catch (COMException)
				{
					Thread.Sleep(ComSleep);
					if (i == 9)
					{
						throw;
					}
				}
			}

			if (ie == null)
			{
				return;
			}

			var disposable = new object();
			var hwnd = new HandleRef(disposable, (IntPtr)ie.HWND);
			_ = SafeNativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
			var processId32 = Convert.ToInt32(processId);
			if (processId32 == 0)
			{
				throw new InvalidOperationException();
			}

			this.ieProcess = Process.GetProcessById(processId32);

			var uri = new Uri(diff.EditPath.ToString().Replace("action=edit", "action=submit"));
			var request = new Request(uri, RequestType.Post, false);
			request
				.Add("wpDiff", "Show changes")
				.Add("wpTextbox1", diff.Text)
				.AddIf("wpMinoredit", "1", diff.IsMinor)
				.Add("wpSummary", diff.EditSummary)
				.Add("wpEditToken", diff.EditToken)
				.Add("wpStarttime", IndexDateTime(diff.StartTimestamp))
				.Add("wpEdittime", IndexDateTime(diff.LastRevisionTimestamp ?? diff.StartTimestamp ?? DateTime.UtcNow));
			var postData = RequestVisitorUrl.Build(request);
			var byteData = Encoding.UTF8.GetBytes(postData);
			var empty = 0;
			var headers = "Content-Type: application/x-www-form-urlencoded";
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

		public bool Validate() => Type.GetTypeFromProgID("InternetExplorer.Application") != null;

		public void Wait() => this.ieProcess?.WaitForExit();
		#endregion

		#region Private Methods
		private static string? IndexDateTime(DateTime? dt) => dt?.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		#endregion
	}
}