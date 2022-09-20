namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal abstract class EsoSkillJob<T> : EditJob
		where T : Skill
	{
		#region Constants
		protected const string SkillTable = "skillTree";
		protected const string MinedTable = "minedSkills";
		private const string TemplateName = "Online Skill Summary";
		#endregion

		#region Static Fields
		private static readonly HashSet<string> DestructionExceptions = new(StringComparer.Ordinal) { "Destructive Touch", "Impulse", "Wall of Elements" };
		private static readonly string[] DestructionTypes = { "Frost", "Shock", "Fire" };
		private static readonly SortedList<string, string> IconNameCache = new(StringComparer.Ordinal);
		#endregion

		#region Fields
		private Dictionary<string, T> skills = new(StringComparer.Ordinal);
		#endregion

		#region Constructors
		protected EsoSkillJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Public Properties
		public bool BigChange { get; set; }
		#endregion

		#region Public Override Properties
		public override string LogName => "Update ESO " + this.TypeText + " Skills";
		#endregion

		#region Protected Override Properties
		protected override Action<EditJob, Page>? EditConflictAction => this.PageLoaded;

		protected override string EditSummary => this.LogName;

		protected override bool MinorEdit => false;

		#endregion

		#region Protected Abstract Properties
		protected abstract string Query { get; }

		protected abstract string TypeText { get; }
		#endregion

		#region Protected Static Methods
		protected static string IconValueFixup(IParameterNode? parameter, string newValue)
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

		protected static string MakeIcon(string lineName, string morphName) => lineName + "-" + morphName;

		protected void UpdateParameter(ITemplateNode template, string name, string value) => this.UpdateParameter(template, name, value, null, null);

		protected void UpdateParameter(ITemplateNode template, string name, string value, TitleCollection? usedList, string? skillName)
		{
			value = value.Trim();
			var factory = new SiteNodeFactory(this.Site);
			var valueNodes = factory.Parse(value);
			if (usedList != null)
			{
				EsoReplacer.ReplaceGlobal(valueNodes);
				EsoReplacer.ReplaceEsoLinks(this.Site, valueNodes);
				EsoReplacer.ReplaceFirstLink(valueNodes, usedList);
				if (skillName != null)
				{
					EsoReplacer.ReplaceSkillLinks(valueNodes, skillName);
				}
			}

			template.Update(name, valueNodes.ToRaw(), ParameterFormat.OnePerLine, true);
		}

		protected void UpdateParameter(ITemplateNode template, string name, string value, bool removeCondition)
		{
			template.ThrowNull();
			if (removeCondition)
			{
				template.Remove(name);
			}
			else
			{
				this.UpdateParameter(template, name, value);
			}
		}
		#endregion

		#region Protected Override Methods

		protected override void AfterLoadPages() => this.GenerateReport();

		protected override void BeforeLoadPages()
		{
			this.StatusWriteLine("Fetching data");
			EsoReplacer.Initialize(this);
			this.skills = this.GetSkillList(0);
			var prevSkills = this.GetSkillList(EsoLog.LatestUpdate - 1);
			foreach (var skill in this.skills)
			{
				if (prevSkills.TryGetValue(skill.Key, out var prevSkill))
				{
					skill.Value.SetBigChange(prevSkill);
				}
			}
		}

		protected override void LoadPages()
		{
			this.skills.ThrowNull();
			TitleCollection titles = new(this.Site);
			foreach (var skill in this.skills)
			{
				titles.Add(skill.Key);
			}

			this.Pages.GetTitles(titles);
		}

		protected override void JobCompleted()
		{
			EsoReplacer.ShowUnreplaced();
			base.JobCompleted();
		}

		protected override void Main()
		{
			base.Main();
			EsoSpace.SetBotUpdateVersion(this, this.TypeText.ToLowerInvariant());
		}

		protected override void PageLoaded(EditJob job, Page page)
		{
			this.skills.ThrowNull();
			this.UpdatePageText(page, this.skills[page.FullPageName]);
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void AddSkillData(T skill, IDataRecord row);

		protected abstract T GetNewSkill(IDataRecord row);

		protected abstract void SkillPostProcess(T skill);

		protected abstract void UpdateSkillTemplate(T skillBase, ITemplateNode template);
		#endregion

		#region Private Static Methods
		private static string NewPage(T skillBase) => "{{Minimal|Skill}}\n{{Online Skill Summary}}\n\n<!--\n==Notes==\n* -->\n{{Stub}}\n{{Online Skills " + skillBase.Class + "}}";
		#endregion

		#region Private Methods
		private void GenerateReport()
		{
			List<string> trivialList = new();
			this.WriteLine($"== {this.TypeText} Skills With Non-Trivial Updates ==");
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
			this.WriteLine($"== {this.TypeText} Skills With Trivial Updates ==");
			this.WriteLine(string.Join(", ", trivialList));
			this.WriteLine();

			SortedList<string, string> iconChanges = new(IconNameCache.Count, StringComparer.Ordinal);
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

		private Dictionary<string, T> GetSkillList(int version)
		{
			Dictionary<string, T> retval = new(StringComparer.Ordinal);
			var query = this.Query;
			if (version > 0)
			{
				var versionText = version.ToStringInvariant();
				query = this.Query
					.Replace("skillTree", "skillTree" + versionText, StringComparison.Ordinal)
					.Replace("minedSkills", "minedSkills" + versionText, StringComparison.Ordinal);
			}

			var errors = false;
			T? baseSkill = null; // We use a string for comparison because the skill itself will sometimes massage the data.
			foreach (var row in Database.RunQuery(EsoLog.Connection, query))
			{
				var newSkill = this.GetNewSkill(row);
				//// var currentName = $"{skill.Class}::{skill.SkillLine}::{skill.Name}";
				if (baseSkill is null ||
					!string.Equals(baseSkill.PageName, newSkill.PageName, StringComparison.Ordinal))
				{
					retval.Add(newSkill.PageName, newSkill);
					baseSkill = newSkill;
				}

				this.AddSkillData(baseSkill, row);
			}

			foreach (var skill in retval)
			{
				this.SkillPostProcess(skill.Value);
				errors |= skill.Value.Check();
			}

			return errors
				? throw new InvalidOperationException("Problems found in skill data.")
				: retval;
		}

		private void UpdatePageText(Page page, T skill)
		{
			if (!page.Exists)
			{
				page.Text = NewPage(skill);
			}

			ContextualParser oldPage = new(page);
			ContextualParser parser = new(page);
			List<SiteTemplateNode> skillSummaries = new(parser.FindSiteTemplates(TemplateName));
			if (skillSummaries.Count != 1)
			{
				this.Warn("Incorrect number of {{" + TemplateName + "}} matches on " + skill.PageName);
			}

			var template = skillSummaries[0];
			template.RemoveDuplicates();
			template.Remove("update");

			this.UpdateParameter(template, "line", skill.SkillLine);
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
				var newValue = IconValueFixup(template.Find(iconName), iconValue + (loopCount > 0 ? FormattableString.Invariant($" ({DestructionTypes[i]})") : string.Empty));
				this.UpdateParameter(template, iconName, newValue);
			}

			this.UpdateSkillTemplate(skill, template);
			template.Sort("titlename", "id", "id1", "id2", "id3", "id4", "id5", "id6", "id7", "id8", "id9", "id10", "line", "type", "icon", "icon2", "icon3", "desc", "desc1", "desc2", "desc3", "desc4", "desc5", "desc6", "desc7", "desc8", "desc9", "desc10", "linerank", "cost", "attrib", "casttime", "range", "radius", "duration", "channeltime", "target", "morph1name", "morph1id", "morph1icon", "morph1desc", "morph2name", "morph2id", "morph2icon", "morph2desc", "image", "imgdesc", "nocat", "notrail");

			EsoReplacer replacer = new(this.Site);
			var newLinks = replacer.CheckNewLinks(oldPage, parser);
			if (newLinks.Count > 0)
			{
				this.Warn(EsoReplacer.ConstructWarning(oldPage, parser, newLinks, "links"));
			}

			var newTemplates = replacer.CheckNewTemplates(oldPage, parser);
			if (newTemplates.Count > 0)
			{
				this.Warn(EsoReplacer.ConstructWarning(oldPage, parser, newTemplates, "templates"));
			}

			parser.UpdatePage();
		}
		#endregion
	}
}