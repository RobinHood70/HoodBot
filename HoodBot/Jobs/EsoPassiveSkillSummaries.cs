namespace RobinHood70.HoodBot.Jobs
{
	using System.Data;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon.Parser;

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
		protected override void AddSkillData(PassiveSkill skill, IDataRecord row)
		{
			var rank = new PassiveRank(row);
			skill.Ranks.Add(rank);
		}

		protected override PassiveSkill GetNewSkill(IDataRecord row) => new(row);

		protected override void SkillPostProcess(PassiveSkill skill)
		{
		}

		protected override void UpdateSkillTemplate(PassiveSkill skillBase, ITemplateNode template)
		{
			skillBase.ThrowNull();
			template.ThrowNull();
			this.UpdateParameter(template, "type", "Passive");
			this.UpdateParameter(template, "id", skillBase.Ranks[^1].Id.ToStringInvariant());
			TitleCollection usedList = new(this.Site);
			foreach (var rank in skillBase.Ranks)
			{
				var splitDescription = Skill.Highlight.Split(rank.Description);
				if (splitDescription[0].Length == 0)
				{
					splitDescription[1] = "<small>(" + splitDescription[1] + ")</small>";
				}

				for (var i = 0; i < splitDescription.Length; i++)
				{
					var coef = Coefficient.FromCollection(rank.Coefficients, splitDescription[i]);
					if (coef != null)
					{
						splitDescription[i] = coef.SkillDamageText();
					}

					// Descriptions used to be done with Join("'''") but in practice, this is unintuitive, so we surround every other value with bold instead.
					if ((i & 1) == 1)
					{
						splitDescription[i] = "'''" + splitDescription[i] + "'''";
					}
				}

				var description = string.Concat(splitDescription);
				var rankText = rank.RankNum.ToStringInvariant();
				var paramName = "desc" + (rank.RankNum == 1 ? string.Empty : rankText);

				this.UpdateParameter(template, paramName, description, usedList, skillBase.Name);
				if (rank is PassiveRank passiveRank)
				{
					this.UpdateParameter(template, "linerank" + rankText, passiveRank.LearnedLevel.ToStringInvariant());
				}
			}
		}
		#endregion
	}
}