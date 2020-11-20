namespace RobinHood70.HoodBot.Jobs
{
	using System.Data;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	internal sealed class EsoPassiveSkillSummaries : EsoSkillJob<PassiveSkill>
	{
		#region Constructors
		[JobInfo("Update Passive Skills", "ESO")]
		public EsoPassiveSkillSummaries(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Propertes
		protected override string Query =>
			@"SELECT
				skillTree.basename, skillTree.skillTypeName,
				minedSkills.id,
				minedSkills.rank,
				minedSkills.learnedLevel,
				minedSkills.coefDescription,
				minedSkills.description,
				a1, b1, c1, R1, type1,
				a2, b2, c2, R2, type2,
				a3, b3, c3, R3, type3
			FROM
				skillTree
					INNER JOIN
				minedSkills ON skillTree.abilityId = minedSkills.id
			WHERE
				minedSkills.isPlayer
					AND minedSkills.isPassive
					AND minedSkills.skillLine != 'Emperor'
			ORDER BY skillTree.basename, skillTree.skillTypeName, minedSkills.rank";

		protected override string TypeText => "Passive";
		#endregion

		#region Protected Override Methods
		protected override PassiveSkill GetNewSkill(IDataRecord row) => new PassiveSkill(row);

		protected override bool UpdateSkillTemplate(PassiveSkill skillBase, ITemplateNode template)
		{
			ThrowNull(skillBase, nameof(skillBase));
			ThrowNull(template, nameof(template));
			var bigChange = false;
			bigChange |= this.TrackedUpdate(template, "type", "Passive");
			bigChange |= this.TrackedUpdate(template, "id", skillBase.Id.ToStringInvariant());
			var usedList = new TitleCollection(this.Site);
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
						if (coef.TypeNumber < -50)
						{
							splitDescription[i] = "(" + splitDescription[i] + " × " + coef.MechanicName + ")";
						}
					}

					// Descriptions used to be done with Join("'''") but in practice, this is unintuitive, so we surround every other value with bold instead.
					if ((i & 1) == 1)
					{
						splitDescription[i] = "'''" + splitDescription[i] + "'''";
					}
				}

				var description = string.Join(string.Empty, splitDescription);
				var rankText = rank.Rank.ToStringInvariant();
				bigChange |= this.TrackedUpdate(template, "desc" + (rank.Rank == 1 ? string.Empty : rankText), description, usedList, skillBase.Name);
				bigChange |= this.TrackedUpdate(template, "linerank" + rankText, rank.LearnedLevel.ToStringInvariant());
			}

			return bigChange;
		}
		#endregion
	}
}