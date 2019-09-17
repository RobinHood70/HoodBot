namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	internal class EsoNpcList : KeyedCollection<long, NPCData>
	{
		#region Public Methods
		public void SortByPageName()
		{
			var list = this.Items as List<NPCData>;
			list.Sort((x, y) => string.Compare(x.PageName, y.PageName, StringComparison.Ordinal));
		}

		public bool TryGetValue(long key, out NPCData item)
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

		#region Protected Override Methods
		protected override long GetKeyForItem(NPCData item) => item?.Id ?? -1;
		#endregion
	}
}
