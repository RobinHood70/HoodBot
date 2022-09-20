namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;

	internal class Rank
	{
		#region Static Fields
		private static readonly Regex BonusFinder = new(@"\s*Current [Bb]onus:.*?\.", RegexOptions.None, Globals.DefaultRegexTimeout);
		#endregion

		#region Constructors
		protected Rank(IDataRecord row)
		{
			this.Coefficients = Coefficient.GetCoefficientList(row);
			this.Id = (int)row["id"];
			this.RankNum = (sbyte)row["rank"];

			var description = (string)row["coefDescription"];
			if (string.IsNullOrWhiteSpace(description))
			{
				description = (string)row["description"];
			}

			if (ReplacementData.IdPartialReplacements.TryGetValue(this.Id, out var partial))
			{
				description = description.Replace(partial.From, partial.To, StringComparison.Ordinal);
			}

			this.Description = HarmonizeDescription(description);
			this.Id = (int)row["id"];
			this.Coefficients = Coefficient.GetCoefficientList(row);
		}
		#endregion

		#region Public Properties
		public IReadOnlyList<Coefficient> Coefficients { get; }

		public string Description { get; }

		public int Id { get; }

		public sbyte RankNum { get; }
		#endregion

		#region Public Sttic Methods
		public static string HarmonizeDescription(string desc) => RegexLibrary.WhitespaceToSpace(BonusFinder.Replace(desc, string.Empty));
		#endregion

		#region Public Methods
		public virtual bool IsBigChange(Rank rank)
		{
			return
				this.RankNum != rank.RankNum ||
				!string.Equals(this.Description, rank.Description, StringComparison.OrdinalIgnoreCase);
		}
		#endregion

	}
}
