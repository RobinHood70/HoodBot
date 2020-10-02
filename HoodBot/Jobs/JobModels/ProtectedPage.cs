namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;

	public sealed partial class ProtectedPage : Page
	{
		#region Fields
		private readonly Dictionary<string, ProtectionEntry> protections = new Dictionary<string, ProtectionEntry>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region Constructors
		public ProtectedPage(ISimpleTitle site)
			: base(site)
		{
		}
		#endregion

		#region Public Properties
		public IReadOnlyDictionary<string, ProtectionEntry> Protections => this.protections;
		#endregion

		#region Protected Override Methods
		protected override void PopulateCustomResults(PageItem pageItem)
		{
			base.PopulateCustomResults(pageItem);
			if (pageItem?.Info != null)
			{
				foreach (var protItem in pageItem.Info.Protections)
				{
					this.protections.Add(protItem.Type, new ProtectionEntry(protItem));
				}
			}
		}
		#endregion
	}
}