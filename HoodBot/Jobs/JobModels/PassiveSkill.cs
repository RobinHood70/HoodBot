namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;

	internal class PassiveSkill : Skill
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
			if (this.Class == "Craft")
			{
				this.Class = "Crafting";
			}

			((List<PassiveRank>)this.Ranks).Add(new PassiveRank(row));
		}
		#endregion

		#region Public Properties
		public int Id => this.Ranks[this.Ranks.Count - 1].Id;

		public IReadOnlyList<PassiveRank> Ranks { get; } = new List<PassiveRank>();
		#endregion

		#region Internal Override Methods
		public override bool Check()
		{
			var retval = false;
			if (this.Ranks.Count < 1 || this.Ranks.Count > 10)
			{
				retval = true;
				Debug.WriteLine($"Warning: {this.Name} has an unusual number of ranks ({this.Ranks.Count}).");
			}

			foreach (var rank in this.Ranks)
			{
				if (string.IsNullOrWhiteSpace(rank.Description))
				{
					retval = true;
					Debug.WriteLine($"Warning: {this.Name} - Rank {rank.Rank} has no description.");
				}
			}

			return retval;
		}
		#endregion
	}
}
