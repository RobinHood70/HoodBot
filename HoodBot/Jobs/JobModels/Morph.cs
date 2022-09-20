﻿namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using RobinHood70.CommonCode;

	internal sealed class Morph
	{
		#region Constructors
		public Morph(IDataRecord row)
		{
			this.Name = (string)row["name"];
			this.CastingTime = EsoSpace.TimeToText((int)row["castTime"]);
			this.EffectLine = (string)row["effectLines"];
			this.Target = (string)row["target"];
		}
		#endregion

		#region Public Properties
		public string CastingTime { get; }

		public string? Description { get; private set; }

		public string EffectLine { get; }

		public string Name { get; }

		public IList<ActiveRank> Ranks { get; } = new List<ActiveRank>(4);

		public string Target { get; }
		#endregion

		#region Public Static Methods
		public static string NowrapSame<T>(IEnumerable<T> values)
			where T : IEquatable<T>, IFormattable =>
			NowrapList(Same(values));

		public static string NowrapSameString(IEnumerable<string> values) =>
			NowrapList(SameString(values));
		#endregion

		#region Public Methods
		public bool IsBigChange(Morph morph)
		{
			if (!string.Equals(this.CastingTime, morph.CastingTime, StringComparison.OrdinalIgnoreCase) ||
				!string.Equals(this.Description, morph.Description, StringComparison.OrdinalIgnoreCase) ||
				!string.Equals(this.EffectLine, morph.EffectLine, StringComparison.OrdinalIgnoreCase) ||
				!string.Equals(this.Target, morph.Target, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			for (var i = 0; i < this.Ranks.Count; i++)
			{
				var rank = this.Ranks[i];
				if (rank.IsBigChange(morph.Ranks[i]))
				{
					return true;
				}
			}

			return false;
		}

		public string RankCosts()
		{
			//// Actual cost is from ComputeEsoSkillCost() in ESO Log. The formula as of this writing is: maxCost * level / 72 + cost.Value / 12, which reduces to cost.Value * (level + 6) / 72). When level is set to a constant of 66 (the maximum level, as used on the wiki), this collapses to just cost.Value.
			var mechanicTexts = new List<string>(this.Ranks.Count);
			var valueTexts = new List<string>(this.Ranks.Count);
			foreach (var rank in this.Ranks)
			{
				var (valueText, mechanicText) = rank.GetCostSplit();
				valueTexts.Add(valueText);
				mechanicTexts.Add(mechanicText);
			}

			var values = NowrapSameString(valueTexts);
			var mechanics = NowrapSameString(mechanicTexts);
			return mechanics.Length == 0
				? values
				: values + ' ' + mechanics;
		}
		#endregion

		#region Public Override Methods
		public override string ToString() => this.Name;
		#endregion

		#region Public Internal Methods
		internal void ParseDescription()
		{
			var splitDescriptions = this.GetDescriptions();
			var splitLength = splitDescriptions[0].Length;

			var errors = false;
			var text = new List<string>();
			for (var i = 0; i < splitLength; i++)
			{
				var data = this.ParseRankDescriptions(splitDescriptions, i);
				var unique = SameString(data);
				if ((i & 1) == 0)
				{
					if (unique.Count != 1)
					{
						Debug.WriteLine($"\nDescription mismatches in {this.Name}.");
						foreach (var rank in this.Ranks)
						{
							Debug.WriteLine($"{rank.Id}: {rank.Description}");
						}

						foreach (var dataItem in data)
						{
							Debug.WriteLine(dataItem);
						}

						errors = true;
					}
					else
					{
						text.Add(unique[0]);
					}
				}
				else
				{
					var addText = NowrapList(unique);
					text.Add(addText);
				}
			}

			if (!errors)
			{
				List<string> descriptions = new();
				for (var i = 0; i < text.Count; i++)
				{
					// Descriptions used to be done with Join("'''") but in practice, this is unintuitive, so we surround every odd-numbered value with bold instead.
					var fragment = text[i];
					descriptions.Add((i & 1) == 1
						? "'''" + fragment + "'''"
						: fragment);
				}

				this.Description = string.Concat(descriptions);
			}
		}
		#endregion

		#region Private Static Methods
		private static string NowrapList(IReadOnlyList<string> list) => list.Count switch
		{
			0 => string.Empty,
			1 => list[0],
			_ => "{{Nowrap|[" + string.Join(" / ", list) + "]}}"
		};

		private static IReadOnlyList<T> SameString<T>(IEnumerable<T> values)
			where T : IEquatable<T>
		{
			var valueList = values.AsReadOnlyList();
			return valueList.Count == 1 || (valueList.Count > 1 && AllEqual())
				? new List<T>(1) { valueList[0] }
				: valueList;

			bool AllEqual()
			{
				var allEqual = true;
				for (var i = 1; i < valueList.Count; i++)
				{
					if (!valueList[i].Equals(valueList[0]))
					{
						allEqual = false;
					}
				}

				return allEqual;
			}
		}

		private static IReadOnlyList<string> Same<T>(IEnumerable<T> values)
			where T : IEquatable<T>, IFormattable
		{
			var valueList = SameString(values);
			var retval = new List<string>(valueList.Count);
			foreach (var value in valueList)
			{
				retval.Add(value.ToString() ?? string.Empty);
			}

			return retval;
		}
		#endregion

		#region Private Methods
		private List<string> ParseRankDescriptions(List<string[]> splitDescriptions, int i)
		{
			var descriptor = string.Empty;
			List<string> retval = new();
			try
			{
				for (var j = 0; j < this.Ranks.Count; j++)
				{
					var coef = Coefficient.FromCollection(this.Ranks[j].Coefficients, splitDescriptions[j][i]);
					if (coef != null)
					{
						splitDescriptions[j][i] = coef.SkillDamageText();
					}

					retval.Add(splitDescriptions[j][i]);
				}
			}
			catch (IndexOutOfRangeException)
			{
				Debug.WriteLine($"Coefficient error on Dave's end in {this.Name}.");
			}

			if (descriptor.Length > 0)
			{
				retval[0] = '(' + retval[0];
				retval[^1] += $" × {descriptor})";
			}

			return retval;
		}

		private List<string[]> GetDescriptions()
		{
			List<string[]> splitDescriptions = new(this.Ranks.Count);
			var splitLength = 0;
			var isBad = false;
			foreach (var rank in this.Ranks)
			{
				var split = Skill.Highlight.Split(rank.Description);
				if (split.Length != splitLength)
				{
					if (splitLength == 0)
					{
						splitLength = split.Length;
					}
					else
					{
						isBad = true;
					}
				}

				splitDescriptions.Add(split);
			}

			if (isBad)
			{
				Debug.WriteLine($"Split lengths not equal for {this.Name}");
				foreach (var abil in this.Ranks)
				{
					Debug.WriteLine($"{abil.Id.ToStringInvariant()}: {abil.Description}");
				}

				throw new InvalidOperationException("Split lengths not equal!");
			}

			return splitDescriptions;
		}
		#endregion
	}
}