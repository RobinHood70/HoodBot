namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using RobinHood70.CommonCode;

internal class Rank
{
	#region Static Fields
	private static readonly Regex BonusFinder = new(@"\s*Current [Bb]onus:.*?(\.|$)", RegexOptions.Multiline | RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);

	private static readonly Regex LeadText = new(@"\A<<.*?>>\s*", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
	#endregion

	#region Constructors
	protected Rank(IDataRecord row, IReadOnlyList<Coefficient> coefficients)
	{
		this.Coefficients = coefficients;
		this.Id = (long)row["abilityId"];
		this.Index = (sbyte)row["rank"];
		this.CastingTime = (int)row["castTime"];
		var description = (string)row["rawDescription"];
		if (description.Length == 0)
		{
			description = (string)row["description"];
		}

		description = EsoLog.ConvertEncoding(description).Trim();
		if (ReplacementData.IdPartialReplacements.TryGetValue(this.Id, out var partial))
		{
			description = description.Replace(partial.From, partial.To, StringComparison.Ordinal);
		}

		description = EsoLog.ColourCode.Replace(description, "'''${content}'''");
		description = BonusFinder.Replace(description, string.Empty);
		var descHeader = EsoLog.ColourCode.Replace(EsoLog.ConvertEncoding((string)row["descHeader"]), "${content}");
		if (descHeader.Length > 0)
		{
			description = $"'''{descHeader}''' " + description;
		}

		this.Description = RegexLibrary.PruneExcessWhitespace(description);
	}
	#endregion

	#region Public Properties
	public int CastingTime { get; }

	public IReadOnlyList<Coefficient> Coefficients { get; }

	public string Description { get; }

	public double Factor { get; protected init; } = 1.0;

	public long Id { get; }

	public sbyte Index { get; }
	#endregion

	#region Public Methods
	public virtual ChangeType GetChangeType(Rank previous)
	{
		if (this.Index != previous.Index)
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