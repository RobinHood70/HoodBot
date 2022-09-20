namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Data;
	using System.Diagnostics;
	using RobinHood70.CommonCode;

	internal sealed class PassiveSkill : Skill
	{
		#region Constructors
		public PassiveSkill(IDataRecord row)
			: base(row)
		{
			this.SkillLine = this.SkillLine
				.Replace(" Skills", string.Empty, StringComparison.Ordinal)
				.Replace("Dark Elf", "Dunmer", StringComparison.Ordinal)
				.Replace("High Elf", "Altmer", StringComparison.Ordinal)
				.Replace("Wood Elf", "Bosmer", StringComparison.Ordinal);
			if (string.Equals(this.Class, "Craft", StringComparison.Ordinal))
			{
				this.Class = "Crafting";
			}
		}
		#endregion

		#region Public Override Methods
		public override bool Check()
		{
			var retval = false;
			if (this.Ranks.Count is < 1 or > 10)
			{
				retval = true;
				Debug.WriteLine($"Warning: {this.Name} has an unusual number of ranks ({this.Ranks.Count}).");
			}

			foreach (var rank in this.Ranks)
			{
				if (string.IsNullOrWhiteSpace(rank.Description))
				{
					retval = true;
					Debug.WriteLine($"Warning: {this.Name} - Rank {rank.RankNum.ToStringInvariant()} has no description.");
				}
			}

			return retval;
		}

		public override void SetBigChange(Skill prev)
		{
			if (prev is not PassiveSkill prevSkill)
			{
				throw new InvalidOperationException();
			}

			var retval = false;
			for (var i = 0; i < this.Ranks.Count; i++)
			{
				retval |= this.Ranks[i].IsBigChange(prevSkill.Ranks[i]);
			}

			this.BigChange = retval;
		}
		#endregion
	}
}
