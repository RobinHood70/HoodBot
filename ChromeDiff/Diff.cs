namespace RobinHood70.ChromeDiff;

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using ChromeDiff.Properties;
using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using RobinHood70.HoodBotPlugins;

[Export(typeof(IPlugin))]
[ExportMetadata("DisplayName", "Chrome")]
public class Diff : IDiffViewer, IDisposable
{
	// This version does things the "right way" by filling in the edit form and clicking the diff button. This is more tolerant of changes in the wiki form, but it requires waiting for the form to load. Note, however, that it does not tolerate changes massive changes, like  Another approach would be to craft the POST request directly, like IeDiff does, which is faster but more brittle. It's unclear if this will be possible with Selenium.
	#region Fields
	private bool disposed;
	private ChromeDriver? driver;
	private int windowCount;
	#endregion

	#region Public Properties
	public string Name => Resources.Name;
	#endregion

	#region Public Methods
	public void Compare(DiffContent diff)
	{
		ObjectDisposedException.ThrowIf(this.disposed, this);
		ArgumentNullException.ThrowIfNull(diff);
		if (diff.EditPath is null)
		{
			throw new ArgumentException("EditPath cannot be null.", nameof(diff));
		}

		// Launch Chrome maximized.
		var options = new ChromeOptions();
		options.AddArgument(Path.Combine($"--user-data-dir={diff.UserFolder}", "Chrome"));
		options.AddArgument("--start-maximized");
		if (diff.UserAgent is not null)
		{
			options.AddArgument($"--user-agent={diff.UserAgent}");
		}

		this.driver = new ChromeDriver(options);

		// Navigate to the edit page.
		this.driver.Navigate().GoToUrl(diff.EditPath);
		this.windowCount = this.driver.WindowHandles.Count;

		// Fill in the form.
		var text = this.WaitForElement(By.Id("wpTextbox1"));
		text.Clear();
		text.SendKeys(diff.Text);
		text = this.WaitForElement(By.Id("wpSummary"));
		text.Clear();
		text.SendKeys(diff.EditSummary);
		if (diff.IsMinor)
		{
			try
			{
				text = this.driver.FindElement(By.Id("wpMinoredit"));
				text.Click();
			}
			catch (NoSuchElementException)
			{
				// This is possible if the bot is logged out currently, in which case the minor edit checkbox is not available. Ignore it and continue.
			}
		}

		text = driver.FindElement(By.Id("wpDiff"));
		text.Submit();
	}

	public bool Validate()
	{
		if (!OperatingSystem.IsWindows())
		{
			return true; // Blindly trust Chrome is installed on non-Windows platforms.
		}

		var registryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe";
		using var machineKey = Registry.LocalMachine.OpenSubKey(registryPath);
		if (machineKey is not null)
		{
			return true;
		}

		using var userKey = Registry.CurrentUser.OpenSubKey(registryPath);
		return userKey is not null;
	}

	public void Wait()
	{
		if (this.driver is null)
		{
			return;
		}

		var pageWait = new WebDriverWait(this.driver, TimeSpan.FromDays(1));
		try
		{
			pageWait.Until(drv => drv.WindowHandles.Count < this.windowCount);
			this.driver.Quit();
		}
		catch (Exception)
		{
		}
	}
	#endregion

	#region Protected Virtual Methods
	protected virtual void Dispose(bool disposing)
	{
		if (!this.disposed)
		{
			if (disposing)
			{
				this.driver?.Dispose();
			}

			this.driver = null;
			this.disposed = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		this.Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	#endregion

	#region Private Methods
	private IWebElement WaitForElement(By condition)
	{
		Debug.Assert(condition is not null);
		Debug.Assert(this.driver is not null);

		var wait = new WebDriverWait(this.driver, TimeSpan.FromSeconds(10));
		return wait.Until(drv =>
		{
			try
			{
				var el = drv.FindElement(condition);
				return el.Displayed ? el : null;
			}
			catch (NoSuchElementException)
			{
				return null;
			}
		});
	}
	#endregion
}