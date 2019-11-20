namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;

	[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated by generic new().")]
	internal class ActiveSkill : Skill
	{
		public ActiveSkill(IDataRecord row)
			: base(row)
		{
			static string FormatRange(int num) => ((double)num / 100).ToString("0.##");

			this.LearnedLevel = (int)row["learnedLevel"];
			this.SkillType = (string)row["type"];

			Morph morph;
			var morphNum = (sbyte)row["morph"];
			if (morphNum == this.Morphs.Count)
			{
				morph = new Morph(row);
				this.Morphs.Add(morph);
			}
			else
			{
				morph = this.Morphs[this.Morphs.Count - 1];
			}

			morph.Abilities.Add(new Ability(row));
			morph.ChannelTimes.Add(EsoGeneral.TimeToText((int)row["channelTime"]));
			morph.Costs.Add((int)row["cost"]);
			morph.Durations.Add(EsoGeneral.TimeToText((int)row["duration"]));
			morph.Radii.Add(FormatRange((int)row["radius"]));
			var maxRange = FormatRange((int)row["maxRange"]);
			var minRange = FormatRange((int)row["minRange"]);
			var range = minRange == "0" ? maxRange : string.Concat(minRange, "-", maxRange);
			morph.Ranges.Add(range);

			if (morph.Abilities.Count == 4)
			{
				morph.ParseDescription();
			}
		}

		#region Public Properties
		public int LearnedLevel { get; }

		public IList<Morph> Morphs { get; } = new List<Morph>(3);

		public string SkillType { get; }
		#endregion

		#region Internal Override Methods
		public override bool Check()
		{
			var retval = false;
			if (this.Morphs.Count != 3)
			{
				retval = true;
				Debug.WriteLine($"Warning: {this.Name} has {this.Morphs.Count} / 3 morphs.");
			}

			foreach (var morph in this.Morphs)
			{
				if (morph.Abilities.Count != 4)
				{
					retval = true;
					Debug.WriteLine($"Warning: {this.Name} - {morph.Name} has {morph.Abilities.Count} / 4 abilities.");
				}

				if (morph.Description == null)
				{
					retval = true;
					Debug.WriteLine($"Warning: {this.Name} - {morph.Name} has no description.");
				}
			}

			return retval;
		}
		#endregion
	}
}