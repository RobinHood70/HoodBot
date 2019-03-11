namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;

	internal class PassiveSkill : Skill
	{
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

		public override void GetData(IDataRecord data)
		{
			base.GetData(data);
			this.SkillLine = this.SkillLine
				.Replace(" Skills", string.Empty)
				.Replace("Dark Elf", "Dunmer")
				.Replace("High Elf", "Altmer")
				.Replace("Wood Elf", "Bosmer");
			if (this.Class == "Craft")
			{
				this.Class = "Crafting";
			}
		}

		public override void GetRankData(IDataRecord data) => (this.Ranks as List<PassiveRank>).Add(new PassiveRank(data));
		#endregion
	}
}
