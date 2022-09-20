namespace RobinHood70.HoodBot.Jobs
{
	using System.Data;
	using RobinHood70.HoodBot.Jobs.JobModels;

	internal sealed class EsoActiveSkillSummaries : EsoSkillJob<ActiveSkill>
	{
		#region Constructors
		[JobInfo("Update Active Skills", "ESO Update")]
		public EsoActiveSkillSummaries(JobManager jobManager)
			: base(jobManager)
		{
			// jobManager.ShowDiffs = false;
		}
		#endregion

		#region Protected Override Properties
		protected override string Query =>
		"SELECT\n" +
			"skillTree.basename,\n" +
			"skillTree.skillTypeName,\n" +
			"skillTree.learnedLevel,\n" +
			"skillTree.type,\n" +
			"minedSkills.id,\n" +
			"minedSkills.name,\n" +
			"minedSkills.castTime,\n" +
			"minedSkills.effectLines,\n" +
			"minedSkills.target,\n" +
			"minedSkills.morph,\n" +
			"minedSkills.rank,\n" +
			"minedSkills.channelTime,\n" +
			"minedSkills.cost,\n" +
			"minedSkills.duration,\n" +
			"minedSkills.maxRange,\n" +
			"minedSkills.minRange,\n" +
			"minedSkills.radius,\n" +
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
			"!minedSkills.isPassive\n" +
			"AND minedSkills.isPlayer = 1\n" +
			"AND minedSkills.morph >= 0\n" +
		"ORDER BY skillTree.baseName, minedSkills.morph, minedSkills.rank;";

		protected override string TypeText => "Active";
		#endregion

		#region Protected Override Methods
		protected override ActiveSkill NewSkill(IDataRecord row) => new(row);
		#endregion
	}
}