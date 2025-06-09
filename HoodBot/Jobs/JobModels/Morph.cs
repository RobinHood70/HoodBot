namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using RobinHood70.CommonCode;

internal sealed class Morph(IDataRecord row, List<Coefficient> coefficients)
{
	#region Public Properties
	public IReadOnlyList<Coefficient> Coefficients => coefficients;

	public string? Description { get; internal set; }

	public string EffectLine { get; } = EsoLog.ConvertEncoding((string)row["effectLines"]);

	public int Index { get; } = (sbyte)row["morph"];

	public string Name { get; } = EsoLog.ConvertEncoding((string)row["name"]);

	public string ParamText => this.Index switch
	{
		0 => string.Empty,
		1 => "1",
		2 => "2",
		_ => throw new InvalidOperationException()
	};

	public IList<ActiveRank> Ranks { get; } = new List<ActiveRank>(4);

	public string Target { get; } = EsoLog.ConvertEncoding((string)row["target"]);
	#endregion

	#region Public Static Methods
	public static string NowrapSame<T>(IEnumerable<T> values)
		where T : IEquatable<T>, IFormattable =>
		NowrapList(Same(values));

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

	public ChangeType GetChangeType(Morph previous)
	{
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

		if (errors)
		{
			return string.Empty;
		}

		List<string> descriptions = [];
		for (var i = 0; i < text.Count; i++)
		{
			// Descriptions used to be done with Join("'''") but in practice, this is unintuitive, so we surround every odd-numbered value with bold instead.
			var fragment = text[i];
			descriptions.Add((i & 1) == 1
				? "'''" + fragment + "'''"
				: fragment);
		}

		return string.Concat(descriptions);
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

	private List<string> ParseRankDescriptions(List<string[]> splitDescriptions, int i)
	{
		var descriptor = string.Empty;
		List<string> retval = [];
		for (var j = 0; j < this.Ranks.Count; j++)
		{
			var coefNum = int.Parse(splitDescriptions[j][i], CultureInfo.InvariantCulture);
			var rank = this.Ranks[j];
			var coef = this.Coefficients[coefNum];
			var text = coef.SkillDamageText(rank.Factor);

			retval.Add(text);
		}

		if (descriptor.Length > 0)
		{
			retval[0] = '(' + retval[0];
			retval[^1] += $" × {descriptor})";
		}

		return retval;
	}
	#endregion
}