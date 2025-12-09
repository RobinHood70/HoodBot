namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
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

internal sealed class NpcData(string dataName, sbyte difficulty, Gender gender, long id, string lootType, string name, PickpocketDifficulty pickpocketDifficulty, string reaction)
{
	#region Static Fields
	private static readonly Dictionary<Gender, string> Genders = new()
	{
		[Gender.None] = "None",
		[Gender.NotApplicable] = "N/A",
		[Gender.Female] = "Female",
		[Gender.Male] = "Male",
	};

	private static readonly Dictionary<PickpocketDifficulty, string> PickpocketDifficulties = new()
	{
		[PickpocketDifficulty.NotApplicable] = "N/A",
		[PickpocketDifficulty.Unknown] = "?",
		[PickpocketDifficulty.Easy] = "Easy",
		[PickpocketDifficulty.Medium] = "Medium",
		[PickpocketDifficulty.Hard] = "Hard",
	};
	#endregion

	#region Public Static Properties
	public static Dictionary<int, string> Reactions { get; } = new()
	{
		[1] = "Hostile",
		[2] = "Neutral",
		[3] = "Friendly",
		[4] = "Player Ally",
		[5] = "NPC Ally",
		[6] = "Companion",
	};
	#endregion

	#region Public Properties
	public string DataName { get; } = dataName;

	public sbyte Difficulty { get; } = difficulty;

	public Gender Gender { get; } = gender;

	public string GenderText => Genders[this.Gender];

	public long Id { get; } = id;

	public string LootType { get; } = lootType;

	public string Name { get; set; } = name;

	public PickpocketDifficulty PickpocketDifficulty { get; } = pickpocketDifficulty;

	public string PickpocketDifficultyText => PickpocketDifficulties[this.PickpocketDifficulty];

	public Dictionary<Place, int> Places { get; } = [];

	public string Reaction { get; } = reaction;

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
			case PlaceType.Unknown:
			case PlaceType.House:
			case PlaceType.Ship:
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