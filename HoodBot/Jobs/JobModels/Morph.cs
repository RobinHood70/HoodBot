namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using RobinHood70.CommonCode;

internal sealed class Morph
{
	#region Constructors
	public Morph(IDataRecord row)
	{
		this.AbilityId = (long)row["abilityId"];
		this.EffectLine = EsoLog.ConvertEncoding((string)row["effectLines"]);
		this.Index = (sbyte)row["morph"];
		this.MaxRank = (sbyte)row["maxRank"];
		this.Name = EsoLog.ConvertEncoding((string)row["name"]);
		this.Target = EsoLog.ConvertEncoding((string)row["target"]);
	}
	#endregion

	#region Public Properties
	public long AbilityId { get; }

	public string? Description { get; internal set; }

	public string EffectLine { get; }

	public int Index { get; }

	public sbyte MaxRank { get; }

	public string Name { get; }

	public string ParamText => this.Index switch
	{
		0 => string.Empty,
		1 => "1",
		2 => "2",
		_ => throw new InvalidOperationException()
	};

	public IList<ActiveRank> Ranks { get; } = new List<ActiveRank>(4);

	public string Target { get; }
	#endregion

	#region Public Static Methods
	public static string NowrapSameString(IEnumerable<string> values) =>
		NowrapList(SameString(values));
	#endregion

	#region Public Methods
	public List<Cost> ConsolidateCosts()
	{
		//// Actual cost is from ComputeEsoSkillCost() in ESO Log. The formula as of this writing is: maxCost * level / 72 + cost.Value / 12, which reduces to cost.Value * (level + 6) / 72). When level is set to a constant of 66 (the maximum level, as used on the wiki), this collapses to just cost.Value.
		var newCosts = new List<Cost>();
		var costs = this.Ranks[0].Costs;
		var values = new List<string>();
		for (var i = 0; i < costs.Count; i++)
		{
			var baseCost = costs[i];
			foreach (var rank in this.Ranks)
			{
				if (rank.Costs.Count != costs.Count)
				{
					throw new InvalidOperationException("Cost count mismatch");
				}

				var currentCost = rank.Costs[i];
				if (!currentCost.MechanicText.OrdinalEquals(baseCost.MechanicText))
				{
					throw new InvalidOperationException("Mechanic mismatch");
				}

				values.Add(rank.Costs[i].Value);
			}

			var newValue = NowrapSameString(values);
			newCosts.Add(new Cost(newValue, baseCost.MechanicText));
		}

		return newCosts;
	}

	public ChangeType GetChangeType(Morph? previous)
	{
		if (previous is null)
		{
			return ChangeType.Major;
		}

		// Descriptions are handled via the ranks, which allows it to compare original descriptions.
		if (!this.EffectLine.OrdinalICEquals(previous.EffectLine) ||
			!this.Target.OrdinalICEquals(previous.Target))
		{
			return ChangeType.Major;
		}

		for (var i = 0; i < this.Ranks.Count; i++)
		{
			if (this.Ranks[i].CastingTime != previous.Ranks[i].CastingTime)
			{
				return ChangeType.Major;
			}
		}

		// TODO: Re-examine this to see if it captures all cases. Also, it's comparing based on both text and post-parsed values whereas it should probably be done by comparing values directly.
		var retval = ChangeType.None;
		var curDesc = this.GetParsedDescription();
		var prevDesc = previous.GetParsedDescription();
		if (!curDesc.OrdinalEquals(prevDesc))
		{
			retval = ChangeType.Minor;
		}

		if (this.Ranks.Count != previous.Ranks.Count)
		{
			Debug.WriteLine($"[[Online:{this.Name}]] changed # of ranks between current and previous version. This is probably a sign of a bug somewhere.");
			return ChangeType.Major;
		}

		for (var i = 0; i < this.Ranks.Count; i++)
		{
			var curRank = this.Ranks[i];
			var prevRank = previous.Ranks[i];
			var changeType = curRank.GetChangeType(prevRank);
			if (changeType > retval)
			{
				if (changeType == ChangeType.Major)
				{
					return ChangeType.Major;
				}

				retval = changeType;
			}
		}

		return retval;
	}

	public bool IsValid()
	{
		if (this.Description is null)
		{
			Debug.WriteLine($"{this.Name} has no description.");
			return false;
		}

		if (this.Ranks[^1].Index != this.MaxRank)
		{
			Debug.WriteLine($"Warning: {this.Name} has the wrong maximum rank ({this.Ranks[^1].Index} vs. {this.MaxRank}).");
			return false;
		}

		return true;
	}
	#endregion

	#region Public Override Methods
	public override string ToString() => this.Name;
	#endregion

	#region Public Internal Methods
	internal string GetParsedDescription()
	{
		var splitDescriptions = this.GetDescriptions();
		var splitLength = splitDescriptions[0].Length;

		var errors = false;
		var sb = new StringBuilder();
		for (var i = 0; i < splitLength; i++)
		{
			var (values, suffix) = this.ParseRankDescriptions(splitDescriptions, i);
			var unique = SameString(values);
			if ((i & 1) == 0)
			{
				if (unique.Count != 1)
				{
					Debug.WriteLine($"\nDescription mismatches in {this.Name}.");
					foreach (var rank in this.Ranks)
					{
						Debug.WriteLine($"{rank.Id}: {rank.Description}");
					}

					foreach (var dataItem in values)
					{
						Debug.WriteLine(dataItem);
					}

					errors = true;
				}
				else
				{
					sb.Append(unique[0]);
				}
			}
			else
			{
				var text = NowrapList(unique);
				var allNumeric = true;
				foreach (var value in unique)
				{
					if (!float.TryParse(value, CultureInfo.InvariantCulture, out var _))
					{
						allNumeric = false;
						break;
					}
				}

				if (allNumeric)
				{
					sb
						.Append("'''")
						.Append(text)
						.Append("'''")
						.Append(suffix);
				}
				else
				{
					sb.Append(text);
				}
			}
		}

		return errors
			? string.Empty
			: sb.ToString();
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
			? [valueList[0]]
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

	private static List<string> Same<T>(IEnumerable<T> values)
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
	private List<string[]> GetDescriptions()
	{
		List<string[]> splitDescriptions = new(this.Ranks.Count);
		var splitLength = 0;
		var isBad = false;
		foreach (var rank in this.Ranks)
		{
			var split = Coefficient.RawCoefficient.Split(rank.Description);
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

	private (List<string> Values, string? DamageType) ParseRankDescriptions(List<string[]> splitDescriptions, int arrayIndex)
	{
		List<string> retval = [];
		string? finalType = null;
		for (var rankNum = 0; rankNum < this.Ranks.Count; rankNum++)
		{
			var text = splitDescriptions[rankNum][arrayIndex];
			if ((arrayIndex & 1) == 1)
			{
				var rank = this.Ranks[rankNum];
				var coefNum = int.Parse(text, CultureInfo.InvariantCulture) - 1;
				var coef = rank.Coefficients[coefNum];
				text = coef.SkillDamageText(rank.Factor);
				if (coef.CoefficientType == -75)
				{
					var split = EsoLog.FloatFinder.Split(text, 2);
					if (split.Length == 3)
					{
						Debug.Assert(split[0].Length == 0, "Number isn't first - this will need both prefix and suffix handling!");
						text = split[1];
						if (finalType is null)
						{
							finalType = split[2];
						}
						else if (!finalType.OrdinalEquals(split[2]))
						{
							Debug.WriteLine($"{coef.AbilityId}, {coef.Index}");
							throw new InvalidOperationException("Multiple suffixes");
						}
					}
				}
				else if (coef.IsDamage)
				{
					var damageSuffix = " " + Coefficient.DamageTypes[coef.DamageType] + " Damage";
					if (finalType is null)
					{
						finalType = damageSuffix;
					}
					else if (!finalType.OrdinalEquals(damageSuffix))
					{
						throw new InvalidOperationException("Multiple damage types");
					}
				}
			}

			retval.Add(text);
		}

		return (retval, finalType);
	}
	#endregion
}