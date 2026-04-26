namespace RobinHood70.ChromeDiff;

using System;
using System.ComponentModel.Composition;
using ChromeDiff.Properties;
using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using RobinHood70.HoodBotPlugins;

[Export(typeof(IPlugin))]
[ExportMetadata("DisplayName", "Chrome")]
public class Diff : IDiffViewer
{
	#region Public Properties
	public string Name => Resources.Name;
	#endregion

	#region Public Methods
	[STAThread]
	public void Compare(DiffContent diff)
	{
		ArgumentNullException.ThrowIfNull(diff);
		if (diff.EditPath is null)
		{
			throw new ArgumentException("EditPath cannot be null.", nameof(diff));
		}

		// Launch Chrome maximized.
		var options = new ChromeOptions();
		options.AddArgument("--start-maximized");
		using var driver = new ChromeDriver(options);

		// Navigate to the edit page.
		driver.Navigate().GoToUrl(diff.EditPath);
		var windowCount = driver.WindowHandles.Count;

		// Fill in the edit form.
		var text = driver.FindElement(By.Name("wpTextbox1"));
		text.Clear();
		text.SendKeys(diff.Text);
		text = driver.FindElement(By.Name("wpSummary"));
		text.Clear();
		text.SendKeys(diff.EditSummary);
		if (diff.IsMinor)
		{
			text = driver.FindElement(By.Name("wpMinoredit"));
			text.Click();
		}

		text.Submit();

		var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
		wait.Until(driver2 => driver2.WindowHandles.Count < windowCount);
		driver.Quit();
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
	}
	#endregion
}