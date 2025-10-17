namespace RobinHood70.HoodBot.Jobs.JobModels;

using System.Collections.Generic;
using System.Data;
using System.Globalization;

internal sealed class PassiveRank(IDataRecord row, List<Coefficient> coefficients) : Rank(row, coefficients)
{
	#region Public Properties
	public int LearnedLevel { get; } = (int)row["learnedLevel"];
	#endregion

	#region Private Methods
	public string FormatDescription()
	{
		var retval = new List<string>();
		var splitDescription = Coefficient.RawCoefficient.Split(this.Description);
		for (var i = 0; i < splitDescription.Length; i++)
		{
			var text = splitDescription[i];
			if ((i & 1) == 1)
			{
				var coefNum = int.Parse(splitDescription[i], CultureInfo.InvariantCulture) - 1;
				var coef = this.Coefficients[coefNum];
				text = coef.SkillDamageText(this.Factor);
				text = coef.CoefficientType == -75
					? EsoLog.FloatFinder.Replace(text, "'''$&'''")
					: $"'''{text}'''{coef.DamageSuffix}";
			}

			retval.Add(text);
		}

		return string.Concat(retval);
	}
	#endregion
}