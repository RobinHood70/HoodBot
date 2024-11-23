namespace RobinHood70.HoodBot;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Design;
using RobinHood70.HoodBot.Models;
using RobinHood70.HoodBot.ViewModels;

public class UserSettings : IJsonSettings<UserSettings>
{
	#region Fields
	private string botDataFolder = DefaultBotDataFolder;
	#endregion

	#region Constructors
	public UserSettings()
	{
		this.BotDataFolder = DefaultBotDataFolder;
	}
	#endregion

	#region Public Static Properties
	public static string DefaultBotDataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BotData");
	#endregion

	#region Public Properties
	public string BotDataFolder
	{
		get => this.botDataFolder;
		set
		{
			if (value != null)
			{
				this.botDataFolder = value;
				Environment.SetEnvironmentVariable("BotData", value);
			}
		}
	}

	public IDictionary<string, string> ConnectionStrings { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

	// TODO: Add this to Load/Save when re-writing Settings class.
	public string? ContactInfo { get; set; } = "robinhood70@live.ca";

	public string FileName => Path.Combine(App.UserFolder, "Settings.json");

	public string? SelectedName { get; set; }

	public ObservableCollection<WikiInfoViewModel> Wikis { get; } = [];
	#endregion

	#region Public Methods
	public WikiInfoViewModel? GetCurrentItem()
	{
		if (this.SelectedName == null)
		{
			return null;
		}

		// It is assumed the list will be relatively small and therefore relatively trivial to iterate through, but this is a function to indicate that it's not completely trivial.
		foreach (var wiki in this.Wikis)
		{
			if (wiki.DisplayName.OrdinalEquals(this.SelectedName))
			{
				return wiki;
			}
		}

		return null;
	}

	public void FromJson(JToken json)
	{
		ArgumentNullException.ThrowIfNull(json);
		this.BotDataFolder = (string?)json[nameof(this.BotDataFolder)] is string botFolder && IsPathValid(botFolder)
			? botFolder
			: DefaultBotDataFolder;
		if (json[nameof(this.ConnectionStrings)] is JObject connectionStrings)
		{
			foreach (var node in connectionStrings.Children<JProperty>())
			{
				var text = (string?)node.Value ?? string.Empty;
				try
				{
					text = Settings.Encrypter.Decrypt(text);
				}
				catch (CryptographicException)
				{
					// Do nothing
				}
				catch (FormatException)
				{
					// Do nothing
				}

				this.ConnectionStrings.Add(node.Name, text);
			}
		}

		if (json[nameof(this.Wikis)] is JArray wikiNode)
		{
			foreach (var node in wikiNode)
			{
				var wiki = JsonSubSetting<WikiInfo>.FromJson(node);
				this.Wikis.Add(new WikiInfoViewModel(wiki));
			}
		}

		this.SelectedName = (string?)json[nameof(this.SelectedName)];
		this.GetCurrentItem();
	}

	public void RemoveWiki(WikiInfoViewModel item)
	{
		ArgumentNullException.ThrowIfNull(item);
		var index = this.Wikis.IndexOf(item);
		if (index >= 0)
		{
			if (this.SelectedName.OrdinalEquals(item.DisplayName))
			{
				this.SelectedName = null;
			}

			this.Wikis.RemoveAt(index);
			Settings.Save(this);
		}
	}

	public JToken ToJson()
	{
		JArray wikis = [];
		foreach (var wiki in this.Wikis)
		{
			wikis.Add(wiki.WikiInfo.ToJson());
		}

		JObject connectionStrings = [];
		foreach (var connectionString in this.ConnectionStrings)
		{
			connectionStrings.Add(connectionString.Key, Settings.Encrypter.Encrypt(connectionString.Value));
		}

		return new JObject
		{
			{ nameof(this.BotDataFolder), new JValue(this.BotDataFolder) },
			{ nameof(this.ConnectionStrings), connectionStrings },
			{ nameof(this.ContactInfo), new JValue(this.ContactInfo) },
			{ nameof(this.SelectedName), new JValue(this.SelectedName) },
			{ nameof(this.Wikis), wikis }
		};
	}
	#endregion

	#region Private Static Methods
	private static bool IsPathValid(string filename)
	{
		try
		{
			Path.GetFullPath(filename);
			return true;
		}
		catch (ArgumentException)
		{
			// Do nothing
		}
		catch (NotSupportedException)
		{
			// Do nothing
		}
		catch (PathTooLongException)
		{
			// Do nothing
		}

		return false;
	}
	#endregion
}