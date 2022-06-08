namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;

	[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated by generic new().")]
	internal sealed class ActiveSkill : Skill
	{
		public ActiveSkill(IDataRecord row)
			: base(row)
		{
			this.SkillType = (string)row["type"];
		}

		#region Public Properties
		public int LearnedLevel { get; private set; }

		public IList<Morph> Morphs { get; } = new List<Morph>(3);

		public string SkillType { get; }
		#endregion

		#region Internal Override Methods
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

		public override void GetData(IDataRecord row)
		{
			static string FormatRange(int num) => ((double)num / 100).ToString("0.##", CultureInfo.InvariantCulture);

			this.LearnedLevel = (int)row["learnedLevel"];

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
			morph.ChannelTimes.Add(EsoSpace.TimeToText((int)row["channelTime"]));
			morph.Costs.Add((int)row["cost"]);
			morph.Durations.Add(EsoSpace.TimeToText((int)row["duration"]));
			morph.Radii.Add(FormatRange((int)row["radius"]));
			var maxRange = FormatRange((int)row["maxRange"]);
			var minRange = FormatRange((int)row["minRange"]);
			var range = string.Equals(minRange, "0", System.StringComparison.Ordinal) ? maxRange : string.Concat(minRange, "-", maxRange);
			morph.Ranges.Add(range);
			morph.ParseDescription();
		}
		#endregion
	}
}