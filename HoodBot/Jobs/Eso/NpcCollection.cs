namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using static RobinHood70.WikiCommon.Globals;

	internal class NpcCollection : KeyedCollection<long, NpcData>
	{
		#region Public Methods
		public void Sort()
		{
			var list = (List<NpcData>)this.Items;
			list.Sort((x, y) => string.Compare(x.PageName, y.PageName, StringComparison.Ordinal));
		}

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
