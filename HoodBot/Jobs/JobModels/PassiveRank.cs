namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Data;

	internal sealed class PassiveRank : Rank
	{
		public PassiveRank(IDataRecord data)
			: base(data)
		{
			this.LearnedLevel = (int)data["learnedLevel"];
		}

		public int LearnedLevel { get; }
	}
}
