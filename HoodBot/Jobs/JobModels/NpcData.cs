namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;

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

	internal sealed class NpcData
	{
		#region Static Fields
		private static readonly Dictionary<sbyte, string> Reactions = new Dictionary<sbyte, string>
		{
			[1] = "Hostile",
			[2] = "Neutral",
			[3] = "Friendly",
			[4] = "Player Ally",
			[5] = "NPC Ally",
		};
		#endregion

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
			var reaction = (sbyte)row["reaction"];
			this.Reaction = reaction == -1
				? this.LootType switch
				{
					"Bard" => "Friendly",
					"" => string.Empty,
					null => string.Empty,
					_ => "Justice Neutral"
				}
				: Reactions[reaction];

			if (name.Length > 2 && name[^2] == '^' && gender == -1)
			{
				var genderChar = char.ToUpperInvariant(name[^1]);
				name = name[0..^2];
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

			this.Name = ReplacementData.NpcNameFixes.TryGetValue(name, out var newName) ? newName : name;
			this.PageName = this.Name;
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

		public Dictionary<string, int> UnknownLocations { get; } = new Dictionary<string, int>(StringComparer.Ordinal);
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
						if (subPlace.Key.PlaceType != PlaceType.Unknown && string.Equals(subPlace.Key.Zone, place.TitleName, StringComparison.Ordinal))
						{
							return true;
						}
					}

					return false;
				});

			var showPlaces = new Dictionary<PlaceType, List<Place>>();
			foreach (var kvp in this.Places)
			{
				var placeType = kvp.Key.PlaceType;
				if (placeType != PlaceType.Unknown)
				{
					if (!showPlaces.TryGetValue(placeType, out var list))
					{
						list = new List<Place>();
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
					Debug.Write($"[[Online:{this.PageName}|{this.Name}]] has multiple {placeType.Key} entries: {string.Join(", ", placeType.Value)}.");
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

			var list = new List<string>();
			switch (placeType)
			{
				case PlaceType.City:
				case PlaceType.Settlement:
				case PlaceType.Store:
					foreach (var item in subset)
					{
						if (item.TitleName != null)
						{
							list.Add(item.TitleName);
						}
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
			var list = new List<Place>();
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
