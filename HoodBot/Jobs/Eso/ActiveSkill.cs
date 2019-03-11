namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;

	[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Instantiated by generic new().")]
	internal class ActiveSkill : Skill
	{
		#region Public Properties
		public int LearnedLevel { get; private set; }

		public IList<Morph> Morphs { get; } = new List<Morph>(3);

		public string SkillType { get; private set; }
		#endregion

		#region Internal Override Methods
		public override void GetData(IDataRecord data)
		{
			base.GetData(data);
			this.LearnedLevel = (int)data["learnedLevel"];
			this.SkillType = (string)data["type"];
		}

		public override void GetRankData(IDataRecord data)
		{
			string FormatRange(int num) => ((double)num / 100).ToString("0.##");

			Morph morph;
			var morphNum = (sbyte)data["morph"];
			if (morphNum == this.Morphs.Count)
			{
				morph = new Morph(data);
				this.Morphs.Add(morph);
			}
			else
			{
				morph = this.Morphs[this.Morphs.Count - 1];
			}

			morph.Abilities.Add(new Ability(data));
			morph.ChannelTimes.Add(EsoGeneral.TimeToText((int)data["channelTime"]));
			morph.Costs.Add((int)data["cost"]);
			morph.Durations.Add(EsoGeneral.TimeToText((int)data["duration"]));
			morph.Radii.Add(FormatRange((int)data["radius"]));
			var maxRange = FormatRange((int)data["maxRange"]);
			var minRange = FormatRange((int)data["minRange"]);
			var range = minRange == "0" ? maxRange : string.Concat(minRange, "-", maxRange);
			morph.Ranges.Add(range);

			if (morph.Abilities.Count == 4)
			{
				morph.ParseDescription();
			}
		}

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