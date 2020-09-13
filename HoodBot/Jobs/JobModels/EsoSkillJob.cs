﻿namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	internal abstract class EsoSkillJob<T> : EditJob
		where T : Skill
	{
		#region Constants
		protected const string SkillTable = "skillTree";
		protected const string MinedTable = "minedSkills";
		private const string TemplateName = "Online Skill Summary";
		#endregion

		#region Static Fields
		private static readonly string[] DestructionTypes = { "Frost", "Shock", "Fire" };

		private static readonly SortedList<string, string> IconNameCache = new SortedList<string, string>();
		private static readonly HashSet<string> DestructionExceptions = new HashSet<string>(StringComparer.Ordinal) { "Destructive Touch", "Impulse", "Wall of Elements" };
		#endregion

		#region Fields
		private readonly SortedSet<Page> nonTrivialChanges = new SortedSet<Page>(TitleComparer<Page>.Instance);
		private readonly Dictionary<string, T> skills = new Dictionary<string, T>(StringComparer.Ordinal);
		private readonly SortedSet<Page> trivialChanges = new SortedSet<Page>(TitleComparer<Page>.Instance);
		#endregion

		#region Constructors
		protected EsoSkillJob(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Update ESO " + this.TypeText + " Skills";
		#endregion

		#region Protected Abstract Properties
		protected abstract string Query { get; }

		protected abstract string TypeText { get; }
		#endregion

		#region Protected Static Methods
		protected static string IconValueFixup(string? currentValue, string newValue)
		{
			if (currentValue != null)
			{
				if (IconNameCache.TryGetValue(currentValue, out var oldValue))
				{
					return oldValue;
				}

				IconNameCache.Add(currentValue, newValue);
			}

			return newValue;
		}

		protected static string MakeIcon(string lineName, string morphName) => lineName + "-" + morphName;

		protected static bool TrackedUpdate(ITemplateNode template, string name, string value) => TrackedUpdate(template, name, value, null, null);

		protected static bool TrackedUpdate(ITemplateNode template, string name, string value, TitleCollection? usedList, string? skillName)
		{
			ThrowNull(template, nameof(template));
			if (!(template.Find(name) is IParameterNode parameter))
			{
				parameter = template.Add(name, string.Empty);
			}

			value = value.Trim();
			var oldValue = parameter.ValueToText();
			if (!string.Equals(oldValue, value, StringComparison.Ordinal))
			{
				parameter.SetValue(value + '\n');

				// We use usedList as the master check, since that should always be available if we're doing checks at all.
				if (usedList != null)
				{
					EsoReplacer.ReplaceGlobal(parameter.Value);
					EsoReplacer.ReplaceEsoLinks(parameter.Value);
					EsoReplacer.ReplaceFirstLink(parameter.Value, usedList);
					if (skillName != null)
					{
						EsoReplacer.ReplaceSkillLinks(parameter.Value, skillName);
					}
				}

				return true;
			}

			return false;
		}

		protected static bool TrackedUpdate(ITemplateNode template, string name, string value, bool removeCondition)
		{
			ThrowNull(template, nameof(template));
			return removeCondition ? template.Remove(name) : TrackedUpdate(template, name, value);
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Fetching data");
			EsoReplacer.Initialize(this);
			this.GetSkillList();
			this.ProgressMaximum = this.skills.Count + 4;
			this.Progress = 3;

			var titles = new TitleCollection(this.Site);
			foreach (var skill in this.skills)
			{
				titles.Add(skill.Key);
			}

			this.StatusWriteLine("Loading pages");
			this.Pages.PageLoaded += this.SkillPageLoaded;
			this.Pages.GetTitles(titles);
			this.Pages.PageLoaded -= this.SkillPageLoaded;
			this.GenerateReport();
		}

		protected override void JobCompleted()
		{
			EsoReplacer.ShowUnreplaced();
			base.JobCompleted();
		}

		protected override void Main()
		{
			this.SavePages(this.LogName, false, this.SkillPageLoaded);
			EsoGeneral.SetBotUpdateVersion(this, this.TypeText.ToLowerInvariant());
			this.Progress++;
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract T GetNewSkill(IDataRecord row);

		protected abstract bool UpdateSkillTemplate(T skillBase, ITemplateNode template);
		#endregion

		#region Private Static Methods
		private static string NewPage(T skillBase) => "{{Minimal|Skill}}\n{{Online Skill Summary}}\n\n<!--\n==Notes==\n* -->\n{{Stub}}\n{{Online Skills " + skillBase.Class + "}}";
		#endregion

		#region Private Methods
		private void GenerateReport()
		{
			if (this.trivialChanges.Count > 0)
			{
				this.WriteLine($"== {this.TypeText} Skills With Trivial Updates ==");
				var newList = new List<string>();
				foreach (var page in this.trivialChanges)
				{
					newList.Add(new SiteLink(page).ToString());
				}

				this.WriteLine(string.Join(", ", newList));
				this.WriteLine();
			}

			if (this.nonTrivialChanges.Count > 0)
			{
				this.WriteLine($"== {this.TypeText} Skills With Non-Trivial Updates ==");
				foreach (var page in this.nonTrivialChanges)
				{
					this.WriteLine($"* {{{{Pl|{page.FullPageName}|{page.LabelName()}|diff=cur}}}}");
				}

				this.WriteLine();
			}

			var iconChanges = new SortedList<string, string>(IconNameCache.Count);
			foreach (var kvp in IconNameCache)
			{
				if (!string.Equals(kvp.Key, kvp.Value, StringComparison.Ordinal))
				{
					iconChanges.Add(kvp.Key, kvp.Value);
				}
			}

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

		private void GetSkillList()
		{
			var errors = false;
			T? currentSkill = null;
			string? lastName = null; // We use a string for comparison because the skill itself will sometimes massage the data.
			foreach (var row in EsoGeneral.RunQuery(this.Query))
			{
				var currentName = (string)row["skillTypeName"] + "::" + (string)row["baseName"];
				if (!string.Equals(lastName, currentName, StringComparison.Ordinal))
				{
					lastName = currentName;
					currentSkill = this.GetNewSkill(row);
					try
					{
						this.skills.Add(currentSkill.PageName, currentSkill);
					}
					catch (InvalidOperationException e)
					{
						this.Warn(e.Message);
						errors = true;
					}
				}

				currentSkill!.GetData(row);
			}

			foreach (var checkSkill in this.skills)
			{
				errors |= checkSkill.Value.Check();
			}

			if (errors)
			{
				throw new InvalidOperationException("Problems found in skill data.");
			}
		}

		private void SkillPageLoaded(object sender, Page page)
		{
			var nonTrivial = this.UpdatePageText(page, this.skills[page.FullPageName]);
			if (sender != this && page.TextModified)
			{
				if (nonTrivial)
				{
					this.nonTrivialChanges.Add(page);
				}
				else
				{
					this.trivialChanges.Add(page);
				}
			}
		}

		private bool UpdatePageText(Page page, T skill)
		{
			if (!page.Exists)
			{
				page.Text = NewPage(skill);
			}

			var parser = new ContextualParser(page);
			var skillSummaries = new List<SiteTemplateNode>(parser.FindTemplates(TemplateName));
			if (skillSummaries.Count != 1)
			{
				this.Warn("Incorrect number of {{" + TemplateName + "}} matches on " + skill.PageName);
			}

			var template = skillSummaries[0];
			template.RemoveDuplicates();
			template.Remove("update");

			var bigChange = false;
			bigChange |= TrackedUpdate(template, "line", skill.SkillLine);
			var iconValue = MakeIcon(skill.SkillLine, skill.Name);

			// Special cases
			if (string.Equals(iconValue, "Woodworking-Woodworking", StringComparison.Ordinal))
			{
				iconValue = "Woodworking-Woodworking Skill";
			}

			if (skill.Name.StartsWith("Keen Eye: ", StringComparison.Ordinal))
			{
				iconValue = iconValue.Split(TextArrays.Colon)[0];
			}

			var loopCount = DestructionExceptions.Contains(skill.Name) ? 2 : 0;
			for (var i = 0; i <= loopCount; i++)
			{
				var iconName = "icon" + (i > 0 ? (i + 1).ToStringInvariant() : string.Empty);
				var iconParamater = template.Find(iconName);
				var newValue = IconValueFixup(iconParamater?.ValueToText(), iconValue + (loopCount > 0 ? FormattableString.Invariant($" ({DestructionTypes[i]})") : string.Empty));
				bigChange |= TrackedUpdate(template, iconName, newValue);
			}

			bigChange |= this.UpdateSkillTemplate(skill, template);
			template.Sort("titlename", "id", "id1", "id2", "id3", "id4", "id5", "id6", "id7", "id8", "id9", "id10", "line", "type", "icon", "icon2", "icon3", "desc", "desc1", "desc2", "desc3", "desc4", "desc5", "desc6", "desc7", "desc8", "desc9", "desc10", "linerank", "cost", "attrib", "casttime", "range", "radius", "duration", "channeltime", "target", "morph1name", "morph1id", "morph1icon", "morph1desc", "morph2name", "morph2id", "morph2icon", "morph2desc", "image", "imgdesc", "nocat", "notrail");

			page.Text = parser.GetText();

			return bigChange;
		}
		#endregion
	}
}