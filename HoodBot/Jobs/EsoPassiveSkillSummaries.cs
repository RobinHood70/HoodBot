namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Eso;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	internal class EsoPassiveSkillSummaries : EsoSkillJob<PassiveSkill>
	{
		#region Constructors
		[JobInfo("Update Passive Skills", "ESO")]
		public EsoPassiveSkillSummaries(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => this.Site.EditingDisabled = true;
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
					AND skillLine != 'Emperor'
			ORDER BY skillTree.basename, skillTree.skillTypeName, minedSkills.rank";

		protected override string TypeText => "Passive";
		#endregion

		#region Protected Override Methods
		protected override void UpdateSkillTemplate(PassiveSkill skillBase, Template template, HashSet<string> replacements)
		{
			ThrowNull(skillBase, nameof(skillBase));
			ThrowNull(template, nameof(template));
			ThrowNull(replacements, nameof(replacements));
			template.AddOrChange("type", "Passive");
			template.AddOrChange("id", skillBase.Id.ToStringInvariant());
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
				}

				var description = string.Join("'''", splitDescription);
				description = EsoReplacer.ReplaceGlobal(description, skillBase.Name);
				description = EsoReplacer.ReplaceLink(description);
				var rankText = rank.Rank.ToStringInvariant();
				template.AddOrChange("desc" + (rank.Rank == 1 ? string.Empty : rankText), description);
				template.AddOrChange("linerank" + rankText, rank.LearnedLevel);
			}
		}
		#endregion
	}
}