namespace RobinHood70.HoodBot
{
	using System.Collections.ObjectModel;
	using System.IO;
	using Newtonsoft.Json;

	public class WikiList
	{
		#region Constructors
		public WikiList(string location) => this.Location = location;
		#endregion

		#region Public Properties
		public int LastSelected { get; set; }

		[JsonIgnore]
		public WikiInfo LastSelectedItem => this.LastSelected >= 0 && this.Wikis.Count > this.LastSelected ? this.Wikis[this.LastSelected] : null;

		public string Location { get; set; }

		public ObservableCollection<WikiInfo> Wikis { get; } = new ObservableCollection<WikiInfo>();
		#endregion

		#region Public Static Methods
		public static WikiList Load(string location)
		{
			try
			{
				var input = File.ReadAllText(location);
				var retval = JsonConvert.DeserializeObject<WikiList>(input);
				retval.Location = location; // For older file versions, but also works in case location has been removed from file.
				return retval;
			}
			catch (FileNotFoundException)
			{
			}

			return new WikiList(location);
		}
		#endregion

		#region Public Methods
		public void Remove(WikiInfo item)
		{
			var index = this.Wikis.IndexOf(item);
			if (index >= 0)
			{
				this.Wikis.RemoveAt(index);
				if (index == this.LastSelected)
				{
					this.LastSelected = -1;
				}

				this.Save();
			}
		}

		public void Save()
		{
			var output = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Include });
			File.WriteAllText(this.Location, output);
		}

		public void UpdateLastSelected(WikiInfo item) => this.LastSelected = this.Wikis.IndexOf(item);
		#endregion
	}
}