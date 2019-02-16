namespace RobinHood70.HoodBot
{
	using System;
	using System.Collections.ObjectModel;
	using System.IO;
	using Newtonsoft.Json;

	public class BotSettings
	{
		#region Fields
		private string botDataFolder;
		#endregion

		#region Constructors
		public BotSettings(string location) => this.Location = location;
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

		public int LastSelectedOffset { get; set; }

		[JsonIgnore]
		public WikiInfo LastSelectedWiki => this.LastSelectedOffset >= 0 && this.Wikis.Count > this.LastSelectedOffset ? this.Wikis[this.LastSelectedOffset] : null;

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
					retval.BotDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BotData");
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
		public void RemoveWiki(WikiInfo item)
		{
			var index = this.Wikis.IndexOf(item);
			if (index >= 0)
			{
				this.Wikis.RemoveAt(index);
				if (index == this.LastSelectedOffset)
				{
					this.LastSelectedOffset = -1;
				}

				this.Save();
			}
		}

		public void Save()
		{
			var output = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Include });
			File.WriteAllText(this.Location, output);
		}

		public void UpdateLastSelected(WikiInfo item) => this.LastSelectedOffset = this.Wikis.IndexOf(item);
		#endregion

		#region Private Static Methods
		private static bool IsPathValid(string filename)
		{
			try
			{
				_ = new FileInfo(filename);
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