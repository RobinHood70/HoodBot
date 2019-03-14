namespace RobinHood70.HoodBot.Jobs.Eso
{
	using System;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;

	internal abstract class EsoSkillJob<T> : EditJob
		where T : Skill, new()
	{
		#region Constants
		protected const string SkillTable = "skillTree";
		protected const string MinedTable = "minedSkills";
		private const string TemplateName = "Online Skill Summary";
		#endregion

		#region Static Fields
		private static readonly string[] DestructionTypes = new string[] { "Frost", "Shock", "Fire" };
		private static readonly HashSet<string> UpdatedParameters = new HashSet<string> { "area", "casttime", "channelTime", "cost", "desc", "desc1", "desc2", "duration", "icon", "icon2", "icon3", "id", "line", "linerank", "morph1name", "morph1id", "morph1icon", "morph1desc", "morph2name", "morph2id", "morph2icon", "morph2desc", "radius", "range", "target", "type" };

		private static SortedList<string, string> iconNameCache = new SortedList<string, string>();
		private static HashSet<string> destructionExceptions = new HashSet<string> { "Destructive Touch", "Impulse", "Wall of Elements" };
		private static Regex skillSummaryFinder = Template.Find(TemplateName);
		#endregion

		#region Fields
		private readonly SortedSet<Page> trivialChanges = new SortedSet<Page>(TitleComparer<Page>.Instance);
		private readonly SortedSet<Page> nonTrivialChanges = new SortedSet<Page>(TitleComparer<Page>.Instance);
		private PageCollection skillPages;
		private IReadOnlyDictionary<string, T> skills;
		#endregion

		#region Constructors
		public EsoSkillJob(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogName => "Update ESO " + this.TypeText + " Skills";
		#endregion

		#region Protected Properties
		protected string PatchVersion { get; private set; }
		#endregion

		#region Protected Abstract Properties
		protected abstract string Query { get; }

		protected abstract string TypeText { get; }
		#endregion

		#region Protected Static Methods
		protected static string IconValueFixup(string currentValue, string newValue)
		{
			if (currentValue != null)
			{
				if (iconNameCache.TryGetValue(currentValue, out var oldValue))
				{
					return oldValue;
				}

				iconNameCache.Add(currentValue, newValue);
			}

			return newValue;
		}

		protected static string MakeIcon(string lineName, string morphName) => lineName + "-" + morphName;
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.StatusWriteLine("Saving pages");
			this.EditConflictAction = this.SkillPageLoaded;
			this.skillPages.Sort();
			foreach (var skillPage in this.skillPages)
			{
				this.SavePage(skillPage, this.LogName, false);
				this.Progress++;
			}

			EsoGeneral.SetBotUpdateVersion(this, this.TypeText.ToLowerInvariant());
			this.Progress++;
		}

		protected override void OnCompleted()
		{
			EsoReplacer.ShowUnreplaced();
			base.OnCompleted();
		}

		protected override void PrepareJob()
		{
			this.PatchVersion = EsoGeneral.GetPatchVersion(this);
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
			this.skillPages = new PageCollection(this.Site);
			this.skillPages.PageLoaded += this.SkillPageLoaded;
			this.skillPages.GetTitles(titles);
			this.skillPages.PageLoaded -= this.SkillPageLoaded;
			this.GenerateReport();
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void UpdateSkillTemplate(T skillBase, Template template, HashSet<string> replacements);
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
					this.WriteLine($"* {{{{Pl|{page.FullPageName}|{page.LabelName}|diff=cur}}}}");
				}

				this.WriteLine();
			}

			var iconChanges = new SortedList<string, string>(iconNameCache.Count);
			foreach (var kvp in iconNameCache)
			{
				if (kvp.Key != kvp.Value)
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
			var allSkills = new Dictionary<string, T>();
			var errors = false;
			T skill = null;
			string lastSkill = null; // We use a string for comparison because the skill itself will sometimes massage the data.
			foreach (var row in EsoGeneral.RunEsoQuery(this.Query))
			{
				var uniqueName = (string)row["baseName"] + "::" + (string)row["skillTypeName"];
				if (lastSkill != uniqueName)
				{
					lastSkill = uniqueName;
					skill = new T();
					skill.GetData(row);
					allSkills.Add(skill.PageName, skill);
				}

				try
				{
					skill.GetRankData(row);
				}
				catch (InvalidOperationException e)
				{
					this.Warn(e.Message);
					errors = true;
				}
			}

			foreach (var checkSkill in allSkills)
			{
				errors |= checkSkill.Value.Check();
			}

			if (errors)
			{
				throw new InvalidOperationException("Problems found in skill data.");
			}

			this.skills = allSkills;
		}

		private void SkillPageLoaded(object sender, Page page)
		{
			var nonTrivial = this.UpdatePageText(page, this.skills[page.FullPageName]);
			if (sender != this)
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

			// EsoReplacer.ClearReplacementStatus();
			if (skillSummaryFinder.Matches(page.Text).Count != 1)
			{
				this.Warn("Incorrect number of {{" + TemplateName + "}} matches on " + skill.PageName);
			}

			var match = skillSummaryFinder.Match(page.Text);
			var replacements = new HashSet<string>();
			var template = Template.Parse(match.Value);

			template.RemoveDuplicates();
			template.Remove("update");
			template.NameParameter.TrailingWhiteSpace = "\n";
			template.DefaultValueFormat.TrailingWhiteSpace = "\n";

			var oldParameters = new ParameterCollection();
			foreach (var paramName in UpdatedParameters)
			{
				var param = template[paramName];
				if (param != null)
				{
					oldParameters.Add(new Parameter(paramName, param.Value));
				}
			}

			template.AddOrChange("line", skill.SkillLine);
			var iconValue = MakeIcon(skill.SkillLine, skill.Name);

			// Special cases
			if (iconValue == "Woodworking-Woodworking")
			{
				iconValue = "Woodworking-Woodworking Skill";
			}

			if (skill.Name.StartsWith("Keen Eye: ", StringComparison.Ordinal))
			{
				iconValue = iconValue.Split(':')[0];
			}

			var loopCount = destructionExceptions.Contains(skill.Name) ? 2 : 0;
			for (var i = 0; i <= loopCount; i++)
			{
				var iconName = "icon" + (i > 0 ? (i + 1).ToStringInvariant() : string.Empty);
				var newValue = iconValue;
				if (loopCount > 0)
				{
					newValue += FormattableString.Invariant($" ({DestructionTypes[i]})");
				}

				newValue = IconValueFixup(template[iconName]?.Value, newValue);
				template.AddOrChange(iconName, newValue);
			}

			this.UpdateSkillTemplate(skill, template, replacements);
			template.Sort("titlename", "id", "id1", "id2", "id3", "id4", "id5", "id6", "id7", "id8", "id9", "id10", "line", "type", "icon", "icon2", "icon3", "desc", "desc1", "desc2", "desc3", "desc4", "desc5", "desc6", "desc7", "desc8", "desc9", "desc10", "linerank", "cost", "attrib", "casttime", "range", "radius", "duration", "channeltime", "target", "morph1name", "morph1id", "morph1icon", "morph1desc", "morph2name", "morph2id", "morph2icon", "morph2desc", "image", "imgdesc", "nocat", "notrail");

			var newText = template.ToString();
			newText = EsoReplacer.FirstLinksOnly(this.Site, newText);
			page.Text = page.Text.Remove(match.Index, match.Length).Insert(match.Index, newText);

			template = Template.Parse(newText);
			var bigChange = false;
			foreach (var parameter in template)
			{
				if (UpdatedParameters.Contains(parameter.Name))
				{
					var oldParameter = oldParameters[parameter.Name];
					if (oldParameter != null)
					{
						// Not optimized to return true immediately because Compare shows useful info in Debug mode.
						bigChange |= EsoReplacer.CompareReplacementText(this, oldParameter.Value, parameter.Value, skill.PageName);
					}
					else
					{
						bigChange = true;
					}
				}
			}

			return bigChange;
		}
		#endregion
	}
}