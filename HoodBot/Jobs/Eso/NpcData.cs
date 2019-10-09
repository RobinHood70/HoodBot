namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using RobinHood70.Robby;

	#region Public Enumerations
	public enum PickpocketDifficulty
	{
		NotApplicable = -1,
		Unknown = 0,
		Easy,
		Medium,
		Hard,
	}
	#endregion

	internal class NpcData
	{
		#region Fields
		private string reaction;
		#endregion

		#region Constructors
		public NpcData(IDataRecord row)
		{
			this.Id = (long)row["id"];
			var name = (string)row["name"];
			var gender = (sbyte)row["gender"];
			this.Difficulty = (sbyte)row["difficulty"];
			this.Difficulty--;
			this.PickpocketDifficulty = (PickpocketDifficulty)(sbyte)row["ppDifficulty"];
			this.LootType = (string)row["ppClass"];

			if (ReplacementData.NpcNameFixes.TryGetValue(name, out var newName))
			{
				name = newName;
			}

			if (name.Length > 2 && name[name.Length - 2] == '^')
			{
				var genderChar = char.ToUpperInvariant(name[name.Length - 1]);
				name = name.Substring(0, name.Length - 2);
				if (gender == -1)
				{
					gender = genderChar == 'M' ? (sbyte)2 : genderChar == 'F' ? (sbyte)1 : (sbyte)-1;
				}
			}

			this.Name = name;
			this.PageName = name;
			this.Gender = (Gender)gender;
		}
		#endregion

		#region Public Properties
		public Dictionary<string, int> AllLocations { get; } = new Dictionary<string, int>();

		public IDictionary<Place, int> AllPlaces { get; set; } = new Dictionary<Place, int>();

		public sbyte Difficulty { get; }

		public Gender Gender { get; }

		public long Id { get; }

		public string LootType { get; }

		public string Name { get; }

		public string PageName { get; set; }

		public PickpocketDifficulty PickpocketDifficulty { get; }

		public string Reaction
		{
			get => this.reaction ?? (this.LootType == "Bard" ? "Friendly" : "Justice Neutral");
			set => this.reaction = value;
		}
		#endregion

		#region Public Methods
		public void TrimPlaces(PlaceCollection places)
		{
			var remove = new List<Place>();
			foreach (var kvp in this.AllPlaces)
			{
				var place = kvp.Key;
				if (place.Zone != null)
				{
					if (places.TryGetValue(place.Zone, out var zonePlace))
					{
						if (zonePlace.Type == PlaceType.Unknown)
						{
							remove.Add(zonePlace);
						}
					}
					else
					{
						Debug.WriteLine("Zone not found: " + place.Zone);
					}
				}
			}

			foreach (var item in remove)
			{
				this.AllPlaces.Remove(item);
			}

			var hasPlaceType = new List<PlaceType>();
			var showPlaceTypes = new List<PlaceType>();
			foreach (var kvp in this.AllPlaces)
			{
				var placeType = kvp.Key.Type;
				if (hasPlaceType.Contains(placeType))
				{
					showPlaceTypes.Add(placeType);
				}
				else
				{
					hasPlaceType.Add(placeType);
				}
			}

			foreach (var placeType in showPlaceTypes)
			{
				var value = string.Empty;
				foreach (var kvp in this.AllPlaces)
				{
					if (kvp.Key.Type == placeType)
					{
						value += ", " + kvp.Key.TitleName;
					}
				}

				Debug.Write($"[[Online:{this.PageName}|{this.Name}]] has multiple {placeType} entries: {value.Substring(2)}.");
			}
		}
		#endregion

		#region Public Override Methods
		public string GetParameterValue(PlaceType placeType, int variesCount)
		{
			var subset = this.Subset(placeType);
			if (subset.Count > variesCount)
			{
				return "Varies";
			}

			var list = new List<string>();
			switch (placeType)
			{
				case PlaceType.City:
				case PlaceType.Settlement:
				case PlaceType.Store:
					foreach (var item in subset)
					{
						list.Add(item.TitleName);
					}

					break;
				default:
					foreach (var item in subset)
					{
						list.Add(item.Title == null ? item.TitleName : SiteLink.LinkTextFromTitle(item.Title));
					}

					break;
			}

			return string.Join(", ", list);
		}

		public override string ToString() => this.Name;
		#endregion

		#region Private Methods
		private IReadOnlyCollection<Place> Subset(PlaceType placeType)
		{
			var retval = new PlaceCollection();
			foreach (var place in this.AllPlaces)
			{
				if (place.Key.Type == placeType)
				{
					retval.Add(place.Key);
				}
			}

			return retval;
		}
		#endregion
	}
}
