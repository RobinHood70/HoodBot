namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;

internal sealed class PlaceCollection
{
	#region Fields
	private readonly HashSet<string> ambiguousNames = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Place> placesByKey = new(StringComparer.Ordinal);
	private readonly Dictionary<string, Place> placesByTitle = new(StringComparer.Ordinal);
	#endregion

	#region Public Indexers
	public Place? this[string name] =>
		this.placesByKey.TryGetValue(name, out var place) ||
		this.placesByTitle.TryGetValue(name, out place)
			? place
			: null;
	#endregion

	#region Public Methods
	public void Add(Place place)
	{
		if (place.TitleName is string titleName && !this.ambiguousNames.Contains(titleName))
		{
			if (this.placesByTitle.Remove(titleName))
			{
				this.ambiguousNames.Add(titleName);
			}
			else
			{
				this.placesByTitle.Add(titleName, place);
			}
		}

		this.placesByKey.Add(place.Key, place);
	}
	#endregion
}