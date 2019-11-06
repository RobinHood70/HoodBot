namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using RobinHood70.Robby;

	#region Public Enumerations
	public enum PickpocketDifficulty
	{
		NotApplicable = -1,
		Unknown,
		Easy,
		Medium,
		Hard,
	}
	#endregion

	internal class NpcData
	{
		#region Constructors
		[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1122:Use string.Empty for empty strings", Justification = "False hit. Cannot use non-constant expression.")]
		public NpcData(IDataRecord row)
		{
			this.Id = (long)row["id"];
			var name = (string)row["name"];
			var gender = (sbyte)row["gender"];
			this.Difficulty = (sbyte)row["difficulty"];
			this.Difficulty--;
			this.PickpocketDifficulty = (PickpocketDifficulty)(sbyte)row["ppDifficulty"];
			this.LootType = (string)row["ppClass"];
			this.Reaction = (sbyte)row["reaction"] switch
			{
				-1 => this.LootType switch
				{
					"Bard" => "Friendly",
					"" => string.Empty,
					null => string.Empty,
					_ => "Justice Neutral"
				},
				1 => "Hostile",
				2 => "Neutral",
				3 => "Friendly",
				_ => throw new InvalidOperationException("Reaction value is out of range.")
			};

			if (name.Length > 2 && name[name.Length - 2] == '^' && gender == -1)
			{
				var genderChar = char.ToUpperInvariant(name[name.Length - 1]);
				name = name.Substring(0, name.Length - 2);
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

			if (ReplacementData.NpcNameFixes.TryGetValue(name, out var newName))
			{
				name = newName;
			}

			this.Name = name;
			this.PageName = name;
		}
		#endregion

		#region Public Properties
		public sbyte Difficulty { get; }

		public Gender Gender { get; }

		public long Id { get; }

		public string LootType { get; }

		public string Name { get; }

		public string PageName { get; set; }

		public PickpocketDifficulty PickpocketDifficulty { get; }

		public Dictionary<Place, int> Places { get; } = new Dictionary<Place, int>();

		public string Reaction { get; }

		public Dictionary<string, int> UnknownLocations { get; } = new Dictionary<string, int>();
		#endregion

		#region Public Methods
		public void TrimPlaces()
		{
			void Remove(Func<Place, int, bool> condition)
			{
				var remove = new List<Place>();
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
			Remove((place, count) => count < quartile);
			Remove(
				(place, count) =>
				{
					foreach (var subPlace in this.Places)
					{
						if (subPlace.Key.Type != PlaceType.Unknown && subPlace.Key.Zone == place.TitleName)
						{
							return true;
						}
					}

					return false;
				});

			var showPlaces = new Dictionary<PlaceType, List<Place>>();
			foreach (var kvp in this.Places)
			{
				var place = kvp.Key;
				if (!showPlaces.TryGetValue(place.Type, out var list))
				{
					list = new List<Place>();
					showPlaces.Add(place.Type, list);
				}

				list.Add(kvp.Key);
			}

			foreach (var placeType in showPlaces)
			{
				if (placeType.Value.Count > 1)
				{
					Debug.Write($"[[Online:{this.PageName}|{this.Name}]] has multiple {placeType.Key} entries: {string.Join(", ", placeType.Value)}.");
				}
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
		private List<Place> Subset(PlaceType placeType)
		{
			var list = new List<Place>();
			foreach (var place in this.Places.Keys)
			{
				if (place.Type == placeType)
				{
					list.Add(place);
				}
			}

			return list;
		}
		#endregion
	}
}
