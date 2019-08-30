namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	internal class EsoNpcList : KeyedCollection<long, NPCData>
	{
		public void SortByPageName()
		{
			var list = this.Items as List<NPCData>;
			list.Sort((x, y) => string.Compare(x.PageName, y.PageName, StringComparison.Ordinal));
		}

		protected override long GetKeyForItem(NPCData item) => item?.Id ?? -1;
	}
}
