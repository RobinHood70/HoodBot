namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using RobinHood70.WallE.Base;
	using static RobinHood70.CommonCode.Globals;

	public sealed class ProtectionEntry
	{
		public ProtectionEntry(ProtectionsItem item)
		{
			ThrowNull(item, nameof(item));
			this.Expiry = item.Expiry;
			this.Level = item.Level;
		}

		public DateTime? Expiry { get; }

		public string Level { get; }
	}
}