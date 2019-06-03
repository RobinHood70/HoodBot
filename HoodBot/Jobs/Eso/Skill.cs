namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System;
	using System.Data;
	using System.Diagnostics;
	using System.Text.RegularExpressions;

	internal abstract class Skill
	{
		#region Static Fields
		private static readonly string[] DoubleColonSplit = new string[] { "::" };
		#endregion

		#region Public Properties
		public string Class { get; protected set; }

		public string Name { get; private set; }

		public string PageName { get; private set; }

		public string SkillLine { get; protected set; }
		#endregion

		#region Internal Static Properties
		internal static Regex Highlight => new Regex(@"\|c[0-9a-fA-F]{6}|\|r");
		#endregion

		#region Public Override Methods
		public override string ToString() => $"{this.Name} ({this.SkillLine})";
		#endregion

		#region Public Virtual Methods
		public virtual void GetData(IDataRecord data)
		{
			this.Name = (string)data["baseName"];
			var classLine = ((string)data["skillTypeName"]).Split(DoubleColonSplit, StringSplitOptions.None);
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

		#region Public Abstract Methods
		public abstract bool Check();

		public abstract void GetRankData(IDataRecord data);
		#endregion
	}
}
