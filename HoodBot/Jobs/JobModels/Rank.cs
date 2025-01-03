﻿namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;

internal class Rank
{
	#region Static Fields
	private static readonly Regex BonusFinder = new(@"\s*Current [Bb]onus:.*?(\.|$)", RegexOptions.Multiline | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);

	private static readonly Regex LeadText = new(@"\A(\|c[0-9a-fA-F]{6})+.*?(\|r)+\s*", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	#endregion

	#region Constructors
	protected Rank(IDataRecord row)
	{
		this.Coefficients = Coefficient.GetCoefficientList(row);
		this.Id = (int)row["id"];
		this.RankNum = (sbyte)row["rank"];

		var description = (string)row["coefDescription"];
		if (description.Length == 0)
		{
			description = (string)row["description"];
		}

		description = EsoLog.ConvertEncoding(description).Trim();
		if (ReplacementData.IdPartialReplacements.TryGetValue(this.Id, out var partial))
		{
			description = description.Replace(partial.From, partial.To, StringComparison.Ordinal);
		}

		description = BonusFinder.Replace(description, string.Empty);
		if (!LeadText.Match(description).Success)
		{
			var descHeader = EsoLog.ConvertEncoding((string)row["descHeader"]);
			if (descHeader.Length > 0)
			{
				description = $"|cffffff{descHeader}|r " + description;
			}
		}

		this.Description = RegexLibrary.PruneExcessWhitespace(description);
	}
	#endregion

	#region Public Properties
	public IReadOnlyList<Coefficient> Coefficients { get; }

	public string Description { get; }

	public int Id { get; }

	public sbyte RankNum { get; }
	#endregion

	#region Public Methods
	public virtual ChangeType GetChangeType(Rank previous)
	{
		if (this.RankNum != previous.RankNum)
		{
			throw new InvalidOperationException("I don't think this is possible.");
		}

		if (this.Description.OrdinalEquals(previous.Description))
		{
			return ChangeType.None;
		}

		var curSkill = Skill.HighlightVar.Replace(this.Description, " ");
		var oldSkill = Skill.HighlightVar.Replace(previous.Description, " ");
		return curSkill.OrdinalICEquals(oldSkill)
			? ChangeType.Minor
			: ChangeType.Major;
	}
	#endregion

}