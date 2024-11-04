namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;

	#region Public Enumerations
	public enum Gender
	{
		None = -1,
		NotApplicable = 0,
		Female = 1,
		Male = 2
	}

	public enum PickpocketDifficulty
	{
		NotApplicable = -1,
		Unknown,
		Easy,
		Medium,
		Hard,
	}
	#endregion

	internal sealed class NpcData
	{
		#region Static Fields
		private static readonly Dictionary<Gender, string> Genders = new()
		{
			[Gender.None] = "None",
			[Gender.NotApplicable] = "N/A",
			[Gender.Female] = "Female",
			[Gender.Male] = "Male",
		};

		private static readonly Dictionary<PickpocketDifficulty, string> PIckpocketDifficulties = new()
		{
			[PickpocketDifficulty.NotApplicable] = "N/A",
			[PickpocketDifficulty.Unknown] = "?",
			[PickpocketDifficulty.Easy] = "Easy",
			[PickpocketDifficulty.Medium] = "Medium",
			[PickpocketDifficulty.Hard] = "Hard",
		};

		private static readonly Dictionary<sbyte, string> Reactions = new()
		{
			[1] = "Hostile",
			[2] = "Neutral",
			[3] = "Friendly",
			[4] = "Player Ally",
			[5] = "NPC Ally",
			[6] = "Companion",
		};
		#endregion

		#region Fields
		private string? name;
		#endregion

		#region Constructors
		public NpcData(IDataRecord row)
		{
			this.Id = (long)row["id"];
			var nameField = EsoLog.ConvertEncoding((string)row["name"]);
			var gender = (sbyte)row["gender"];
			this.Difficulty = (sbyte)row["difficulty"];
			this.Difficulty--;
			this.PickpocketDifficulty = (PickpocketDifficulty)(sbyte)row["ppDifficulty"];
			this.LootType = EsoLog.ConvertEncoding((string)row["ppClass"]);
			var reaction = (sbyte)row["reaction"];
			this.Reaction = reaction == -1
				? this.LootType switch
				{
					"Bard" => "Friendly",
					"" => string.Empty,
					_ => "Justice Neutral"
				}
				: Reactions[reaction];

			if (nameField.Length > 2 && nameField[^2] == '^' && gender == -1)
			{
				var genderChar = char.ToUpperInvariant(nameField[^1]);
				nameField = nameField[0..^2];
				this.Gender = genderChar switch
				{
					'M' => Gender.Male,
					'F' => Gender.Female,
					_ => Gender.None
				};
			}
			else
			{
				this.Gender = (Gender)gender;
			}

			this.DataName = nameField.Trim();
			if (ReplacementData.NpcNameFixes.TryGetValue(nameField, out var newName))
			{
				this.name = newName;
			}
		}
		#endregion

		#region Public Properties
		public string DataName { get; }

		public sbyte Difficulty { get; }

		public Gender Gender { get; }

		public string GenderText => Genders[this.Gender];

		public long Id { get; }

		public string LootType { get; }

		public string Name
		{
			get => this.name ?? this.DataName;
			internal set => this.name = value;
		}

		public PickpocketDifficulty PickpocketDifficulty { get; }

		public string PickpocketDifficultyText => PIckpocketDifficulties[this.PickpocketDifficulty];

		public Dictionary<Place, int> Places { get; } = [];

		public string Reaction { get; }

		public Title? Title { get; set; }

		// TODO: This is really just a Dictionary<string, int> but converted to Place in hopes of doing something a little better with Places, cuz it's a mess right now.
		public Dictionary<Place, int> UnknownLocations { get; } = [];
		#endregion

		#region Public Methods
		public void TrimPlaces()
		{
			void Remove(Func<Place, int, bool> condition)
			{
				List<Place> remove = [];
				foreach (var kvp in this.Places)
				{
					if (condition(kvp.Key, kvp.Value))
					{
						remove.Add(kvp.Key);
					}
				}

				foreach (var item in remove)
				{
					this.Places.Remove(item);
				}
			}

			var sum = 0;
			foreach (var kvp in this.Places)
			{
				sum += kvp.Value;
			}

			var quartile = (int)((double)sum / (this.Places.Count * 2));
			Remove((_, count) => count < quartile);
			Remove(
				(place, _) =>
				{
					foreach (var subPlace in this.Places)
					{
						if (subPlace.Key.PlaceType != PlaceType.Unknown && subPlace.Key.Zone.OrdinalEquals(place.TitleName))
						{
							return true;
						}
					}

					return false;
				});

			Dictionary<PlaceType, List<Place>> showPlaces = [];
			foreach (var kvp in this.Places)
			{
				var placeType = kvp.Key.PlaceType;
				if (placeType != PlaceType.Unknown)
				{
					if (!showPlaces.TryGetValue(placeType, out var list))
					{
						list = [];
						showPlaces.Add(placeType, list);
					}

					list.Add(kvp.Key);
				}
			}

			var wroteSomething = false;
			foreach (var placeType in showPlaces)
			{
				if (placeType.Value.Count > 1)
				{
					wroteSomething = true;
					Debug.Write($"[[Online:{this.Title?.PageName ?? this.DataName}|{this.DataName}]] has multiple {placeType.Key} entries: {string.Join(", ", placeType.Value)}.");
				}
			}

			if (wroteSomething)
			{
				Debug.WriteLine(string.Empty);
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

			List<string> list = [];
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
						var title = item.ToString();
						if (title != null)
						{
							list.Add(title);
						}
					}

					break;
			}

			return string.Join(", ", list);
		}

		public override string ToString() => this.Name;
		#endregion

		#region Private Methods
		private List<Place> Subset(PlaceType placeType)
		{
			List<Place> list = [];
			foreach (var place in this.Places.Keys)
			{
				if (place.PlaceType == placeType)
				{
					list.Add(place);
				}
			}

			return list;
		}
		#endregion
	}
}