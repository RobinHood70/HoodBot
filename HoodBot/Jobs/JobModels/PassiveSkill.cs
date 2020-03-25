namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;

	internal class PassiveSkill : Skill
	{
		#region Fields
		private readonly List<PassiveRank> ranks = new List<PassiveRank>();
		#endregion

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
		}
		#endregion

		#region Public Properties
		public int Id => this.ranks[^1].Id;

		public IReadOnlyList<PassiveRank> Ranks => this.ranks;
		#endregion

		#region Public Override Methods
		public override bool Check()
		{
			var retval = false;
			if (this.ranks.Count < 1 || this.ranks.Count > 10)
			{
				retval = true;
				Debug.WriteLine($"Warning: {this.Name} has an unusual number of ranks ({this.Ranks.Count}).");
			}

			foreach (var rank in this.ranks)
			{
				if (string.IsNullOrWhiteSpace(rank.Description))
				{
					retval = true;
					Debug.WriteLine($"Warning: {this.Name} - Rank {rank.Rank} has no description.");
				}
			}

			return retval;
		}

		public override void GetData(IDataRecord row) => this.ranks.Add(new PassiveRank(row));
		#endregion
	}
}
