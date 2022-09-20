namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;

	internal sealed class ActiveSkill : Skill
	{
		#region Constructors
		public ActiveSkill(IDataRecord row)
			: base(row)
		{
			this.LearnedLevel = (int)row["learnedLevel"];
			this.SkillType = (string)row["type"];
		}
		#endregion

		#region Public Properties
		public int LearnedLevel { get; private set; }

		public IList<Morph> Morphs { get; } = new List<Morph>(3);

		public string SkillType { get; internal set; }
		#endregion

		#region Public Override Methods
		public override bool Check()
		{
			foreach (var morph in this.Morphs)
			{
				if (morph.Description == null)
				{
					Debug.WriteLine($"Warning: {this.Name} - {morph.Name} has no description.");
					return true;
				}
			}

			return false;
		}

		public override void SetBigChange(Skill prev)
		{
			if (prev is not ActiveSkill prevSkill)
			{
				throw new InvalidOperationException();
			}

			var bigChange = false;
			for (var i = 0; i < this.Morphs.Count; i++)
			{
				var morph = this.Morphs[i];
				bigChange |= morph.IsBigChange(prevSkill.Morphs[i]);
			}

			this.BigChange = bigChange;
		}

		#endregion
	}
}