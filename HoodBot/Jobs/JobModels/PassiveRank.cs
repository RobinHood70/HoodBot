namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Data;

	internal sealed class PassiveRank(IDataRecord row) : Rank(row)
	{
		#region Public Properties
		public int LearnedLevel { get; } = (int)row["learnedLevel"];
		#endregion
	}
}