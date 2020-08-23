namespace RobinHood70.HoodBot
{
	using System;
	using System.Collections.ObjectModel;
	using System.IO;
	using Newtonsoft.Json;
	using RobinHood70.HoodBot.Models;
	using static RobinHood70.CommonCode.Globals;

	public class BotSettings
	{
		#region Fields
		private string botDataFolder = DefaultBotDataFolder;
		#endregion

		#region Constructors
		public BotSettings(string location) => this.Location = location;
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

		public string? CurrentName { get; set; }

		public string Location { get; set; }

		public ObservableCollection<WikiInfo> Wikis { get; } = new ObservableCollection<WikiInfo>();
		#endregion

		#region Public Static Methods
		public static BotSettings Load(string location)
		{
			try
			{
				var input = File.ReadAllText(location);
				var retval = JsonConvert.DeserializeObject<BotSettings>(input);

				// Set up default values if not loaded from file, or otherwise invalid.
				if (!IsPathValid(retval.Location))
				{
					retval.Location = location; // For older file versions, but also works in case location has been removed from file.
				}

				if (!IsPathValid(retval.BotDataFolder))
				{
					retval.botDataFolder = DefaultBotDataFolder;
				}

				return retval;
			}
			catch (FileNotFoundException)
			{
			}

			return new BotSettings(location);
		}
		#endregion

		#region Public Methods
		public WikiInfo? GetCurrentItem()
		{
			if (this.CurrentName == null)
			{
				return null;
			}

			// It is assumed the list will be relatively small and therefore relatively trivial to iterate through, but this is a function to indicate that it's not completely trivial.
			foreach (var wiki in this.Wikis)
			{
				if (wiki.DisplayName == this.CurrentName)
				{
					return wiki;
				}
			}

			return null;
		}

		public void RemoveWiki(WikiInfo item)
		{
			ThrowNull(item, nameof(item));
			var index = this.Wikis.IndexOf(item);
			if (index >= 0)
			{
				if (this.CurrentName == item.DisplayName)
				{
					this.CurrentName = null;
				}

				this.Wikis.RemoveAt(index);
				this.Save();
			}
		}

		public void Save()
		{
			var output = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Include });
			File.WriteAllText(this.Location, output);
		}

		// TODO: See if there's more that needs to be done for Add. If nothing else, it doesn't sort after adding. Was that intentional?
		public void UpdateCurrentWiki(WikiInfo? wiki) => this.CurrentName = wiki?.DisplayName;
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