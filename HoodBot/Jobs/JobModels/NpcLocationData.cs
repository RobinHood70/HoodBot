namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Data;

public class NpcLocationData
{
	#region Constructors
	public NpcLocationData(IDataRecord row)
	{
		this.Id = (long)row["npcId"];
		this.Zone = EsoLog.ConvertEncoding((string)row["zone"])
			.Replace(" (Normal)", string.Empty, StringComparison.Ordinal)
			.Replace(" (Veteran)", string.Empty, StringComparison.Ordinal);
		this.LocCount = (int)row["locCount"];
	}
	#endregion

	#region Public Properties
	public long Id { get; }

	public string Zone { get; }

	public int LocCount { get; }
	#endregion
}