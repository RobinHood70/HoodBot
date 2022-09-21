﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoSkills : EditJob
	{
		#region Constants
		private const string MinedTable = "minedSkills";
		private const string SkillTable = "skillTree";
		private const string Query =
		"SELECT\n" +
			"skillTree.skillTypeName,\n" +
			"skillTree.learnedLevel,\n" +
			"skillTree.baseName,\n" +
			"skillTree.type,\n" +
			"minedSkills.id,\n" +
			"minedSkills.name,\n" +
			"minedSkills.description,\n" +
			"minedSkills.target,\n" +
			"minedSkills.effectLines,\n" +
			"minedSkills.duration,\n" +
			"minedSkills.cost,\n" +
			"minedSkills.maxRange,\n" +
			"minedSkills.minRange,\n" +
			"minedSkills.radius,\n" +
			"minedSkills.isPassive,\n" +
			"minedSkills.castTime,\n" +
			"minedSkills.channelTime,\n" +
			"minedSkills.mechanic,\n" +
			"minedSkills.rank,\n" +
			"minedSkills.morph,\n" +
			"minedSkills.coefDescription,\n" +
			"minedSkills.type1, minedSkills.a1, minedSkills.b1, minedSkills.c1,\n" +
			"minedSkills.type2, minedSkills.a2, minedSkills.b2, minedSkills.c2,\n" +
			"minedSkills.type3, minedSkills.a3, minedSkills.b3, minedSkills.c3,\n" +
			"minedSkills.type4, minedSkills.a4, minedSkills.b4, minedSkills.c4,\n" +
			"minedSkills.type5, minedSkills.a5, minedSkills.b5, minedSkills.c5,\n" +
			"minedSkills.type6, minedSkills.a6, minedSkills.b6, minedSkills.c6\n" +
		"FROM\n" +
			"skillTree\n" +
		"INNER JOIN\n" +
			"minedSkills ON skillTree.abilityId = minedSkills.id\n" +
		"WHERE\n" +
			"minedSkills.isPlayer AND\n" +
			"minedSkills.morph >= 0 AND\n" +
			"minedSkills.skillLine != 'Emperor'\n" +
		"ORDER BY skillTree.baseName, skillTree.skillTypeName, minedSkills.morph, minedSkills.rank;";
		#endregion

		#region Static Fields
		private static readonly SortedList<string, string> IconNameCache = new(StringComparer.Ordinal);
		#endregion

		#region Fields
		private Dictionary<string, Skill> skills = new(StringComparer.Ordinal);
		#endregion

		#region Constructors
		[JobInfo("Skills", "ESO Update")]
		public EsoSkills(JobManager jobManager)
			: base(jobManager)
		{
			this.MinorEdit = false;
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Update ESO Skills";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => this.LogName;

		#endregion

		#region Public Static Methods
		public static string IconValueFixup(IParameterNode? parameter, string newValue)
		{
			if (parameter != null)
			{
				var currentValue = parameter.Value.ToValue().Trim();
				if (IconNameCache.TryGetValue(currentValue, out var oldValue))
				{
					return oldValue;
				}

				IconNameCache.Add(currentValue, newValue);
			}

			return newValue;
		}
		#endregion

		#region Protected Override Methods
		protected override void AfterLoadPages() => this.GenerateReport();

		protected override void BeforeLoadPages()
		{
			this.StatusWriteLine("Fetching data");
			EsoReplacer.Initialize(this);
			this.skills = GetSkillList(0);
			var prevSkills = GetSkillList(EsoLog.LatestUpdate - 1);
			foreach (var (key, skill) in this.skills)
			{
				if (prevSkills.TryGetValue(key, out var prevSkill))
				{
					skill.SetBigChange(prevSkill);
				}
			}
		}

		protected override void JobCompleted()
		{
			EsoReplacer.ShowUnreplaced();
			base.JobCompleted();
		}

		protected override void LoadPages()
		{
			TitleCollection titles = new(this.Site);
			foreach (var skill in this.skills)
			{
				titles.Add(skill.Key);
			}

			this.Pages.GetTitles(titles);
		}

		protected override void Main()
		{
			base.Main();
			EsoSpace.SetBotUpdateVersion(this, "skills");
		}

		protected override void PageLoaded(EditJob job, Page page) =>
			this.skills[page.FullPageName].UpdatePageText(page, this.Site);
		#endregion

		#region Private Static Methods
		private static SortedList<string, string> GetIconChanges()
		{
			SortedList<string, string> iconChanges = new(IconNameCache.Count, StringComparer.Ordinal);
			foreach (var kvp in IconNameCache)
			{
				if (!string.Equals(kvp.Key, kvp.Value, StringComparison.Ordinal))
				{
					iconChanges.Add(kvp.Key, kvp.Value);
				}
			}

			return iconChanges;
		}

		private static Dictionary<string, Skill> GetSkillList(int version)
		{
			Dictionary<string, Skill> retval = new(StringComparer.Ordinal);
			var versionText = version.ToStringInvariant();
			var query = version == 0
				? Query
				: Query
					.Replace(SkillTable, SkillTable + versionText, StringComparison.Ordinal)
					.Replace(MinedTable, MinedTable + versionText, StringComparison.Ordinal);

			var errors = false;
			Skill? currentSkill = null;
			foreach (var row in Database.RunQuery(EsoLog.Connection, query))
			{
				var isPassive = (sbyte)row["isPassive"] == 1;
				Skill newSkill = isPassive
					 ? new PassiveSkill(row)
					 : new ActiveSkill(row);

				// We use a string for comparison below because the skill itself will sometimes massage the data.
				if (currentSkill is null ||
					!string.Equals(currentSkill.PageName, newSkill.PageName, StringComparison.Ordinal))
				{
					currentSkill = newSkill;
					retval.Add(currentSkill.PageName, currentSkill);
				}

				currentSkill.AddData(row);
			}

			foreach (var (_, skill) in retval)
			{
				skill.PostProcess();
				errors |= skill.Check();
			}

			return errors
				? throw new InvalidOperationException("Problems found in skill data.")
				: retval;
		}
		#endregion

		#region Private Methods
		private void GenerateReport()
		{
			List<string> trivialList = new();
			this.WriteLine($"== Skills With Non-Trivial Updates ==");
			foreach (var skill in this.skills)
			{
				var title = (Title)TitleFactory.FromUnvalidated(this.Site, skill.Key);
				if (skill.Value.BigChange)
				{
					this.WriteLine($"* {{{{Pl|{title.FullPageName}|{title.PipeTrick()}|diff=cur}}}}");
				}
				else
				{
					trivialList.Add(title.AsLink(LinkFormat.LabelName));
				}
			}

			this.WriteLine();
			this.WriteLine($"== Skills With Trivial Updates ==");
			this.WriteLine(string.Join(", ", trivialList));
			this.WriteLine();

			var iconChanges = GetIconChanges();

			if (iconChanges.Count > 0)
			{
				this.WriteLine("== Icon Changes ==");
				this.WriteLine("{| class=\"wikitable\"");
				this.WriteLine("! From !! To");
				foreach (var kvp in iconChanges)
				{
					this.WriteLine($"|-\n| [[:File:ON-icon-skill-{kvp.Key}.png|{kvp.Key}]] || [[:File:ON-icon-skill-{kvp.Value}.png|{kvp.Value}]]");
				}

				this.WriteLine("|}");
			}

			this.WriteLine();
		}
		#endregion
	}
}