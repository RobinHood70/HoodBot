/*
namespace RobinHood70.ChromeDiff;

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using ChromeDiff.Properties;
using Microsoft.Win32;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using RobinHood70.HoodBotPlugins;

[Export(typeof(IPlugin))]
[ExportMetadata("DisplayName", "Chrome")]
public class Diff : IDiffViewer, IDisposable
{
	#region Fields
	private bool disposed;
	ChromeDriver driver;
	#endregion

	#region Constructors
	public Diff()
	{
		// Launch Chrome maximized.
		var options = new ChromeOptions();
		options.AddArgument("--start-maximized");
		options.AddArgument("--user-agent=HoodBot/1.0.0.0 (robinhood70@live.ca) WallE/1.0.0.0");
		this.driver = new ChromeDriver(options);
	}
	#endregion

	#region Public Properties
	public string Name => Resources.Name;
	#endregion

	#region Public Methods
	public void Compare(DiffContent diff)
	{
		ArgumentNullException.ThrowIfNull(diff);
		if (diff.EditPath is null)
		{
			throw new ArgumentException("EditPath cannot be null.", nameof(diff));
		}

		var postData = CreatePostData(diff);
		var submit = diff.EditPath.ToString().Replace("action=edit", "action=submit");
		var js = $"""
			var xhr = new XMLHttpRequest();
			xhr.open('POST', '{diff.EditPath}', false);
			xhr.setRequestHeader('Content-type', 'application/x-www-form-urlencoded');
			xhr.send('{postData}');
			return xhr.response;
			""";
		var retval = driver.ExecuteScript(js);
	}

	public bool Validate()
	{
		if (!OperatingSystem.IsWindows())
		{
			return true; // Blindly trust it's installed on non-Windows platforms.
		}

		var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe";
		using var key = Registry.LocalMachine.OpenSubKey(registryPath) ?? Registry.CurrentUser.OpenSubKey(registryPath);
		return key is not null;
	}

	public void Wait()
	{
		var windowCount = this.driver.WindowHandles.Count;
		var pageWait = new WebDriverWait(driver, TimeSpan.FromDays(1));
		try
		{
			pageWait.Until(drv => drv.WindowHandles.Count < windowCount);
			this.driver.Quit();
		}
		catch (Exception)
		{
		}
	}
	#endregion

	#region Private Methods
	private static string CreatePostData(DiffContent diff)
	{
		var parameters = new Dictionary<string, string?>(StringComparer.Ordinal)
		{
			// ["action"] = "submit",
			// ["title"] = diff.FullPageName,
			// ["wpDiff"] = "Show changes",
			// ["wpTextbox1"] = "Hello", // diff.Text,
			// ["wpMinoredit"] = diff.IsMinor ? "1" : null,
			// ["wpSummary"] = diff.EditSummary,
			// ["wpAutoSummary"] = string.Empty.GetHash(HashType.Md5),
			// ["wpEditToken"] = diff.EditToken,
			// ["wpStarttime"] = IndexDateTime(diff.StartTimestamp),
			// ["wpEdittime"] = IndexDateTime(diff.LastRevisionTimestamp ?? diff.StartTimestamp ?? DateTime.Now),
			// ["wpUltimateParam"] = "1"
		};

		var postData = new StringBuilder();
		foreach (var (key, value) in parameters)
		{
			if (value is not null)
			{
				var value2 = JsonConvert.SerializeObject(value);
				// value2 = value2[1..^1]; // Remove the surrounding quotes added by JsonConvert.SerializeObject.
				postData
					.Append('&')
					.Append(key)
					.Append('=')
					.Append(value2);
			}
		}

		postData.Remove(0, 1);
		return postData.ToString();
	}

	[return: NotNullIfNotNull(nameof(dt))]
	private static string? IndexDateTime(DateTime? dt) => dt?.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				// TODO: dispose managed state (managed objects)
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			disposed = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~Diff()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	#endregion

	#region Private Methods
	private IWebElement FindTextBox(By byText)
	{
		try
		{
			// Wait until textbox is available.
			var wait = new WebDriverWait(this.driver, TimeSpan.FromSeconds(10));
			return wait.Until(drv =>
			{
				try
				{
					var el = drv.FindElement(byText);
					return el.Displayed ? el : null;
				}
				catch (NoSuchElementException)
				{
					return null;
				}
			});
		}
		catch (Exception)
		{
			Console.WriteLine("Element with locator: '" + byText + "' was not found.");
			throw;
		}
	}
	#endregion
}*/