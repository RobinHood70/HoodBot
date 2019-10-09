namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System.Collections.ObjectModel;

	internal class PlaceCollection : KeyedCollection<string, Place>
	{
		#region Public Methods
		public bool TryGetValue(string key, out Place item)
		{
			if (this.Dictionary != null)
			{
				return this.Dictionary.TryGetValue(key, out item);
			}

			foreach (var testItem in this)
			{
				if (this.GetKeyForItem(testItem) == key)
				{
					item = testItem;
					return true;
				}
			}

			item = null;
			return false;
		}
		#endregion

		#region Protected Properties
		protected override string GetKeyForItem(Place item) => item?.Key;
		#endregion
	}
}
