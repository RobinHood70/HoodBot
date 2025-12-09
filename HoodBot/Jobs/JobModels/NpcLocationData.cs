namespace RobinHood70.HoodBot.Jobs.JobModels;

public class NpcLocationData(long id, string zone, int locCount)
{
	#region Public Properties
	public long Id { get; } = id;

	public string Zone { get; } = zone;

	public int LocCount { get; } = locCount;
	#endregion
}