﻿namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using static RobinHood70.CommonCode.Globals;

	internal sealed class NpcCollection : KeyedCollection<long, NpcData>
	{
		#region Public Methods

		public void GetLocations()
		{
			var npcIds = new List<long>(this.Count);
			foreach (var npc in this)
			{
				if (npc.UnknownLocations.Count == 0)
				{
					npcIds.Add(npc.Id);
				}
			}

			if (npcIds.Count > 0)
			{
				foreach (var npc in EsoGeneral.GetNpcLocationData(npcIds))
				{
					var place = new Place(npc.Zone);
					this[npc.Id].UnknownLocations.Add(place, npc.LocCount);
				}
			}
		}

		public void ParseLocations(PlaceCollection places)
		{
			foreach (var npc in this)
			{
				if (npc.Page != null)
				{
					var locCopy = new Dictionary<Place, int>(npc.UnknownLocations);
					foreach (var kvp in locCopy)
					{
						var key = kvp.Key;
						try
						{
							if (places[key.TitleName] is Place place)
							{
								npc.Places.Add(place, kvp.Value);
								npc.UnknownLocations.Remove(key);
							}
							else
							{
								Debug.WriteLine($"Location not found: {key}");
							}
						}
						catch (InvalidOperationException)
						{
							Debug.WriteLine($"Location {key.TitleName} is ambiguous for NPC {npc.Name}");
						}
					}
				}
			}
		}

		public void Sort() => (this.Items as List<NpcData>)?.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

		public NpcData? ValueOrDefault(long key)
		{
			if (this.Dictionary != null)
			{
				return this.Dictionary.TryGetValue(key, out var item) ? item : default;
			}

			foreach (var testItem in this)
			{
				if (this.GetKeyForItem(testItem) == key)
				{
					return testItem;
				}
			}

			return default;
		}
		#endregion

		#region Protected Override Methods
		protected override long GetKeyForItem(NpcData item) => item == null ? throw ArgumentNull(nameof(item)) : item.Id;
		#endregion
	}
}
