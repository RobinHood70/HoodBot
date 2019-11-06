namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System;
	using System.Collections.Generic;

	internal class PlaceCollection
	{
		#region Fields
		private readonly HashSet<string> ambiguousNames = new HashSet<string>();
		private readonly Dictionary<string, Place> primary = new Dictionary<string, Place>();
		private readonly Dictionary<string, Place> secondary = new Dictionary<string, Place>();
		#endregion

		#region Public Indexers
		public Place this[string name]
		{
			get
			{
				if (this.ambiguousNames.Contains(name))
				{
					throw new InvalidOperationException("Tried to look up amibguous name: " + name);
				}

				return
					this.primary.TryGetValue(name, out var place) ? place :
					this.secondary.TryGetValue(name, out place) ? place :
					default;
			}
		}
		#endregion

		#region Public Methods
		public void Add(Place place)
		{
			var titleName = place.TitleName;
			if (!this.ambiguousNames.Contains(titleName) && !this.secondary.ContainsKey(titleName))
			{
				if (this.secondary.ContainsKey(titleName))
				{
					this.ambiguousNames.Add(titleName);
					this.secondary.Remove(titleName);
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
