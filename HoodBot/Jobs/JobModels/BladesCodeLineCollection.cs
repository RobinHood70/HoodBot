namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.ObjectModel;

	public class BladesCodeLineCollection : KeyedCollection<string, BladesCodeLine>
	{
		public BladesCodeLineCollection()
		{
		}

		protected override string GetKeyForItem(BladesCodeLine item) => (item ?? throw new ArgumentNullException(nameof(item))).Name;
	}
}
