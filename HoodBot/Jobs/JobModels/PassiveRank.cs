namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Data;

	internal sealed class PassiveRank : Rank
	{
		public PassiveRank(IDataRecord row)
			: base(row)
		{
			this.LearnedLevel = (int)row["learnedLevel"];
		}

		public int LearnedLevel { get; }
	}
}
