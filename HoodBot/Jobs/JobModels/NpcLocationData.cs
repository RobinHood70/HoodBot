namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Data;

	public class NpcLocationData(IDataRecord row)
	{
		#region Public Properties
		public long Id { get; } = (long)row["npcId"];

		public string Zone { get; } = EsoLog.ConvertEncoding((string)row["zone"]);

		public int LocCount { get; } = (int)row["locCount"];
		#endregion
	}
}