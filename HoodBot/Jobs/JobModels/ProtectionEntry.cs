namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using RobinHood70.WallE.Base;

public sealed class ProtectionEntry
{
	public ProtectionEntry(ProtectionsItem item)
	{
		ArgumentNullException.ThrowIfNull(item);
		this.Expiry = item.Expiry;
		this.Level = item.Level;
	}

	public DateTime? Expiry { get; }

	public string Level { get; }
}