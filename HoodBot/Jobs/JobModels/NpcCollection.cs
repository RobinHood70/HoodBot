namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using static RobinHood70.CommonCode.Globals;

	internal sealed class NpcCollection : KeyedCollection<long, NpcData>
	{
		#region Public Methods
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
