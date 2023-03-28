namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class PassiveSkill : Skill
	{
		#region Fields
		private readonly List<Rank> ranks = new();
		#endregion

		#region Constructors
		public PassiveSkill(IDataRecord row)
			: base(row)
		{
			this.SkillLine = this.SkillLine.Replace(" Skills", string.Empty, StringComparison.Ordinal);
		}
		#endregion

		#region Public Override Methods
		public override void AddData(IDataRecord row)
		{
			var rank = new PassiveRank(row);
			this.ranks.Add(rank);
		}

		public override bool Check()
		{
			var retval = false;
			if (this.ranks.Count is < 1 or > 10)
			{
				retval = true;
				Debug.WriteLine($"Warning: {this.Name} has an unusual number of ranks ({this.ranks.Count}).");
			}

			foreach (var rank in this.ranks)
			{
				if (string.IsNullOrWhiteSpace(rank.Description))
				{
					retval = true;
					Debug.WriteLine($"Warning: {this.Name} - Rank {rank.RankNum.ToStringInvariant()} has no description.");
				}
			}

			return retval;
		}

		public override void SetChangeType(Skill previous)
		{
			if (previous is not PassiveSkill prevSkill)
			{
				throw new InvalidOperationException();
			}

			var retval = ChangeType.None;
			for (var i = 0; i < this.ranks.Count; i++)
			{
				var curRank = this.ranks[i];
				var prevRank = prevSkill.ranks[i];
				var changeType = curRank.GetChangeType(prevRank);
				if (changeType > retval)
				{
					if (changeType == ChangeType.Major)
					{
						this.ChangeType = ChangeType.Major;
						return;
					}

					retval = changeType;
				}
			}

			this.ChangeType = retval;
		}
		#endregion

		#region Protected Override Methods
		protected override void UpdateTemplate(SiteNodeFactory factory, ITemplateNode template)
		{
			template.ThrowNull();
			UpdateParameter(factory, template, "type", "Passive");
			UpdateParameter(factory, template, "id", this.ranks[^1].Id.ToStringInvariant());
			TitleCollection usedList = new(factory.Site);
			foreach (var rank in this.ranks)
			{
				var splitDescription = Highlight.Split(rank.Description);
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

				UpdateParameter(factory, template, paramName, description, usedList, this.Name);
				if (rank is PassiveRank passiveRank)
				{
					UpdateParameter(factory, template, "linerank" + rankText, passiveRank.LearnedLevel.ToStringInvariant());
				}
			}
		}
		#endregion
	}
}
