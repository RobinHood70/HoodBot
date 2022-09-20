namespace RobinHood70.HoodBot.Jobs
{
	using System.Data;
	using RobinHood70.HoodBot.Jobs.JobModels;

	internal sealed class EsoPassiveSkillSummaries : EsoSkillJob<PassiveSkill>
	{
		#region Constructors
		[JobInfo("Update Passive Skills", "ESO Update")]
		public EsoPassiveSkillSummaries(JobManager jobManager)
			: base(jobManager)
		{
			// jobManager.ShowDiffs = false;
		}
		#endregion

		#region Protected Override Propertes
		protected override string Query =>
		"SELECT\n" +
			"skillTree.basename,\n" +
			"skillTree.skillTypeName,\n" +
			"minedSkills.id,\n" +
			"minedSkills.rank,\n" +
			"minedSkills.learnedLevel,\n" +
			"minedSkills.coefDescription,\n" +
			"minedSkills.description,\n" +
			"minedSkills.mechanic,\n" +
			"a1, b1, c1, R1, type1,\n" +
			"a2, b2, c2, R2, type2,\n" +
			"a3, b3, c3, R3, type3,\n" +
			"a4, b4, c4, R4, type4,\n" +
			"a5, b5, c5, R5, type5,\n" +
			"a6, b6, c6, R6, type6\n" +
		"FROM\n" +
			"skillTree\n" +
		"INNER JOIN\n" +
			"minedSkills ON skillTree.abilityId = minedSkills.id\n" +
		"WHERE\n" +
			"minedSkills.isPlayer\n" +
			"AND minedSkills.isPassive\n" +
			"AND minedSkills.skillLine != 'Emperor'\n" +
		"ORDER BY skillTree.basename, skillTree.skillTypeName, minedSkills.rank";

		protected override string TypeText => "Passive";
		#endregion

		#region Protected Override Methods
		protected override PassiveSkill NewSkill(IDataRecord row) => new(row);
		#endregion
	}
}