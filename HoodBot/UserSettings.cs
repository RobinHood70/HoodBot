namespace RobinHood70.HoodBot
{
	using System;
	using System.Collections.ObjectModel;
	using System.IO;
	using Newtonsoft.Json.Linq;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.HoodBot.ViewModels;
	using static RobinHood70.CommonCode.Globals;

	public class UserSettings
	{
		#region Fields
		private string botDataFolder = DefaultBotDataFolder;
		#endregion

		#region Constructors
		private UserSettings(string location, JToken? json)
		{
			this.Location = location;
			if (json == null)
			{
				this.BotDataFolder = DefaultBotDataFolder;
			}
			else
			{
				this.BotDataFolder = (string?)json[nameof(this.BotDataFolder)] ?? DefaultBotDataFolder;
				if (json[nameof(this.Wikis)] is JToken wikiNode && wikiNode.Type == JTokenType.Array)
				{
					foreach (var node in wikiNode)
					{
						var wiki = new WikiInfo(node);
						this.Wikis.Add(new WikiInfoViewModel(wiki));
					}
				}

				this.SelectedName = (string?)json[nameof(this.SelectedName)];
				this.GetCurrentItem();
			}
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

		// TODO: Add this to Load/Save when re-writing Settings class.
		public string? ContactInfo { get; set; } = "robinhood70@live.ca";

		public string Location { get; }

		public string? SelectedName { get; set; }

		public ObservableCollection<WikiInfoViewModel> Wikis { get; } = new ObservableCollection<WikiInfoViewModel>();
		#endregion

		#region Public Static Methods
		public static UserSettings Load(string location)
		{
			try
			{
				var input = File.ReadAllText(location);
				var json = JObject.Parse(input);
				var retval = new UserSettings(location, json);
				if (!IsPathValid(retval.BotDataFolder))
				{
					retval.botDataFolder = DefaultBotDataFolder;
				}

				return retval;
			}
			catch (FileNotFoundException)
			{
				return new UserSettings(location, null);
			}
		}
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
				if (wiki.DisplayName == this.SelectedName)
				{
					return wiki;
				}
			}

			return null;
		}

		public void RemoveWiki(WikiInfoViewModel item)
		{
			ThrowNull(item, nameof(item));
			var index = this.Wikis.IndexOf(item);
			if (index >= 0)
			{
				if (this.SelectedName == item.DisplayName)
				{
					this.SelectedName = null;
				}

				this.Wikis.RemoveAt(index);
				this.Save();
			}
		}

		public void Save()
		{
			var wikis = new JArray();
			foreach (var wiki in this.Wikis)
			{
				wikis.Add(wiki.WikiInfo.ToJson());
			}

			var json = new JObject
			{
				new JProperty(nameof(this.BotDataFolder), this.BotDataFolder),
				new JProperty(nameof(this.SelectedName), this.SelectedName),
				new JProperty(nameof(this.Wikis), wikis)
			};

			File.WriteAllText(this.Location, json.ToString());
		}

		// TODO: See if there's more that needs to be done for Add. If nothing else, it doesn't sort after adding. Was that intentional?
		public void UpdateCurrentWiki(WikiInfoViewModel? wiki) => this.SelectedName = wiki?.DisplayName;
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
			}
			catch (NotSupportedException)
			{
			}
			catch (PathTooLongException)
			{
			}

			return false;
		}
		#endregion
	}
}