namespace RobinHood70.InternetExplorerDiff
{
	using System;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading;
	using RobinHood70.HoodBotPlugins;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon.RequestBuilder;
	using SHDocVw;

	[Description("Visual Studio")]
	public class IeDiff : IDiffViewer
	{
		#region Fields
		private Process ieProcess;
		#endregion

		#region Public Properties
		public string Name => Properties.Resources.Name;
		#endregion

		#region Public Methods
		[STAThread]
		public void Compare(Page page, string editSummary, bool isMinor, string editToken)
		{
			InternetExplorer ie = null;
			for (var i = 0; i < 10; i++)
			{
				try
				{
					ie = new InternetExplorer();
					break;
				}
				catch (COMException)
				{
					Thread.Sleep(500);
					if (i == 9)
					{
						throw;
					}
				}
			}

			var result = SafeNativeMethods.GetWindowThreadProcessId(new IntPtr(ie.HWND), out var processId);
			this.ieProcess = Process.GetProcessById(Convert.ToInt32(processId));

			var urib = new UriBuilder(page.Site.ArticlePath.Replace("/$1", string.Empty))
			{
				Query = "title=" + page.FullPageName.Replace(' ', '_') + "&action=submit"
			};
			var request = new Request(urib.Uri, RequestType.Post, false);
			request
				.Add("wpDiff", "Show changes")
				.Add("wpTextbox1", page.Text)
				.AddIf("wpMinoredit", "1", isMinor)
				.Add("wpSummary", editSummary)
				.Add("wpEditToken", editToken)
				.Add("wpStarttime", IndexDateTime(page.StartTimestamp))
				.Add("wpEdittime", IndexDateTime(page.Revisions.Current?.Timestamp ?? page.StartTimestamp))
				;
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
					Thread.Sleep(500);
				}
			}
			while (error);

			ie.Visible = true;
		}

		public bool ValidatePlugin() => Type.GetTypeFromProgID("InternetExplorer.Application", false) != null;

		public void Wait() => this.ieProcess.WaitForExit();
		#endregion

		#region Private Methods
		private static string IndexDateTime(DateTime? dt) => dt?.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
		#endregion
	}
}