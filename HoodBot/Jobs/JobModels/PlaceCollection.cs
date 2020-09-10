namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;

	internal sealed class PlaceCollection
	{
		#region Fields
		private readonly HashSet<string> ambiguousNames = new HashSet<string>(StringComparer.Ordinal);
		private readonly Dictionary<string, Place> primary = new Dictionary<string, Place>(StringComparer.Ordinal);
		private readonly Dictionary<string, Place> secondary = new Dictionary<string, Place>(StringComparer.Ordinal);
		#endregion

		#region Public Indexers
		public Place? this[string name] =>
			this.ambiguousNames.Contains(name) ? throw new InvalidOperationException("Tried to look up amibguous name: " + name) :
			this.primary.TryGetValue(name, out var place) || this.secondary.TryGetValue(name, out place) ? place :
			default;
		#endregion

		#region Public Methods
		public void Add(Place place)
		{
			if (place.TitleName is string titleName && !this.ambiguousNames.Contains(titleName))
			{
				if (this.secondary.Remove(titleName))
				{
					this.ambiguousNames.Add(titleName);
				}
				else
				{
					this.secondary.Add(titleName, place);
				}
			}

			this.primary.Add(place.Key, place);
		}
		#endregion
	}
}
