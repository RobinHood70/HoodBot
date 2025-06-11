namespace RobinHood70.HoodBot.Jobs.JobModels;

using System.Data;

public class NpcLocationData
{
	#region Constructors
	public NpcLocationData(IDataRecord row)
	{
		this.Id = (long)row["npcId"];
		this.Zone = EsoLog.ConvertEncoding((string)row["zone"]);
		this.LocCount = (int)row["locCount"];
	}
	#endregion

	#region Public Properties
	public long Id { get; }

	public string Zone { get; }

	public int LocCount { get; }
	#endregion
}