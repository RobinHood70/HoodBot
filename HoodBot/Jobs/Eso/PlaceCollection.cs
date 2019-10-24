namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System.Collections.ObjectModel;

	internal class PlaceCollection : KeyedCollection<string, Place>
	{
		#region Public Methods
		public Place ValueOrDefault(string key)
		{
			if (key != null)
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
			}

			return default;
		}
		#endregion

		#region Protected Properties
		protected override string GetKeyForItem(Place item) => item?.Key;
		#endregion
	}
}
