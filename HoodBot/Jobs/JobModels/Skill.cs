namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Data;
	using System.Diagnostics;
	using System.Text.RegularExpressions;
	using static RobinHood70.CommonCode.Globals;

	internal abstract class Skill
	{
		#region Static Fields
		private static readonly string[] DoubleColonSplit = new[] { "::" };
		#endregion

		#region Constructors
		protected Skill(IDataRecord row)
		{
			this.Name = (string)row["baseName"];
			var classLine = ((string)row["skillTypeName"]).Split(DoubleColonSplit, StringSplitOptions.None);
			this.Class = classLine[0];
			this.SkillLine = classLine[1];
			var testName = this.Name;
			if (!ReplacementData.SkillNameFixes.TryGetValue(testName, out var newName))
			{
				testName = this.Name + " - " + this.SkillLine;
				ReplacementData.SkillNameFixes.TryGetValue(testName, out newName);
			}

			if (newName != null)
			{
				Debug.WriteLine("Page Name Changed: {0} => {1}", testName, newName);
			}

			this.PageName = "Online:" + (newName ?? this.Name);
		}
		#endregion

		#region Public Properties
		public string Class { get; protected set; }

		public string Name { get; }

		public string PageName { get; }

		public string SkillLine { get; protected set; }
		#endregion

		#region Internal Static Properties
		internal static Regex Highlight => new Regex(@"\|c[0-9a-fA-F]{6}|\|r", RegexOptions.None, DefaultRegexTimeout);
		#endregion

		#region Public Override Methods
		public override string ToString() => $"{this.Name} ({this.SkillLine})";
		#endregion

		#region Public Abstract Methods
		public abstract bool Check();

		public abstract void GetData(IDataRecord row);
		#endregion
	}
}
