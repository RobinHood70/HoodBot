namespace RobinHood70.HoodBot.Jobs.JobModels
{
	public class NpcLocationData
	{
		public NpcLocationData(long id, string zone, int count)
		{
			this.Id = id;
			this.Zone = zone;
			this.LocCount = count;
		}

		public long Id { get; }

		public string Zone { get; }

		public int LocCount { get; }
	}
}