namespace RobinHood70.InternetExplorerDiff;

using System;
using System.Collections.Generic;
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
		ArgumentNullException.ThrowIfNull(diff);
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

		var ptr = new IntPtr(ie.HWND);
		if (ptr == IntPtr.Zero)
		{
			throw new InvalidOperationException();
		}

		var disposable = new object();
		HandleRef hwnd = new(disposable, ptr);

		_ = SafeNativeMethods.GetWindowThreadProcessId(hwnd, out var processId);
		var processId32 = Convert.ToInt32(processId);
		if (processId32 == 0)
		{
			throw new InvalidOperationException();
		}

		this.ieProcess = Process.GetProcessById(processId32);
		NavigateToDiff(diff, ie);
		_ = SafeNativeMethods.ShowWindow(hwnd, 3);
	}

	public bool Validate() => OperatingSystem.IsWindows() && Type.GetTypeFromProgID("InternetExplorer.Application") != null;

	public void Wait() => this.ieProcess?.WaitForExit();
	#endregion

	#region Private Methods
	private static string CreatePostData(DiffContent diff)
	{
		var parameters = new Dictionary<string, string?>(StringComparer.Ordinal)
		{
			["wpDiff"] = "Show changes",
			["wpTextbox1"] = diff.Text,
			["wpMinoredit"] = diff.IsMinor ? "1" : null,
			["wpSummary"] = diff.EditSummary,
			["wpAutoSummary"] = string.Empty.GetHash(HashType.Md5),
			["wpEditToken"] = diff.EditToken,
			["wpStarttime"] = IndexDateTime(diff.StartTimestamp),
			["wpEdittime"] = IndexDateTime(diff.LastRevisionTimestamp ?? diff.StartTimestamp ?? DateTime.Now),
			["wpUltimateParam"] = "1"
		};

		var postData = new StringBuilder();
		foreach (var (key, value) in parameters)
		{
			if (value is not null)
			{
				postData
					.Append('&')
					.Append(key)
					.Append('=')
					.Append(Globals.EscapeDataString(value));
			}
		}

		postData.Remove(0, 1);
		return postData.ToString();
	}

	[return: NotNullIfNotNull(nameof(dt))]
	private static string? IndexDateTime(DateTime? dt) => dt?.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

	private static void NavigateToDiff(DiffContent diff, InternetExplorer ie)
	{
		const int empty = 0;
		const string headers = "Content-Type: application/x-www-form-urlencoded";

		Globals.ThrowIfNull(diff.EditPath, nameof(diff), nameof(diff.EditPath));
		Uri uri = new(diff.EditPath.ToString().Replace("action=edit", "action=submit", StringComparison.Ordinal));
		var postData = CreatePostData(diff);
		var byteData = Encoding.UTF8.GetBytes(postData);
		var error = true;
		do
		{
			try
			{
				ie.Navigate(uri.AbsoluteUri, empty, empty, byteData, headers);
				error = false;
			}
			catch (COMException)
			{
				Thread.Sleep(ComSleep);
			}
		}
		while (error);
	}

	#endregion
}