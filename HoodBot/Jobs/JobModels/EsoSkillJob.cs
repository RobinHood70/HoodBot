namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	internal abstract class EsoSkillJob<T> : EditJob
		where T : Skill
	{
		#region Constants
		protected const string SkillTable = "skillTree";
		protected const string MinedTable = "minedSkills";
		private const string TemplateName = "Online Skill Summary";
		#endregion

		#region Static Fields
		private static readonly string[] DestructionTypes = new[] { "Frost", "Shock", "Fire" };
		private static readonly HashSet<string> UpdatedParameters = new HashSet<string> { "area", "casttime", "channelTime", "cost", "desc", "desc1", "desc2", "duration", "icon", "icon2", "icon3", "id", "line", "linerank", "morph1name", "morph1id", "morph1icon", "morph1desc", "morph2name", "morph2id", "morph2icon", "morph2desc", "radius", "range", "target", "type" };

		private static readonly SortedList<string, string> IconNameCache = new SortedList<string, string>();
		private static readonly HashSet<string> DestructionExceptions = new HashSet<string> { "Destructive Touch", "Impulse", "Wall of Elements" };
		private static readonly Regex SkillSummaryFinder = Template.Find(TemplateName);
		#endregion

		#region Fields
		private readonly SortedSet<Page> nonTrivialChanges = new SortedSet<Page>(TitleComparer<Page>.Instance);
		private readonly Dictionary<string, T> skills = new Dictionary<string, T>();
		private readonly SortedSet<Page> trivialChanges = new SortedSet<Page>(TitleComparer<Page>.Instance);
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
		protected string? PatchVersion { get; private set; }
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
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			this.SavePages(this.LogName, false, this.SkillPageLoaded);
			EsoGeneral.SetBotUpdateVersion(this, this.TypeText.ToLowerInvariant());
			this.Progress++;
		}

		protected override void JobCompleted()
		{
			EsoReplacer.ShowUnreplaced();
			base.JobCompleted();
		}

		protected override void BeforeLogging()
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
			this.Pages.PageLoaded += this.SkillPageLoaded;
			this.Pages.GetTitles(titles);
			this.Pages.PageLoaded -= this.SkillPageLoaded;
			this.GenerateReport();
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract T GetNewSkill(IDataRecord row);

		protected abstract void UpdateSkillTemplate(T skillBase, Template template);
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
					newList.Add(new SiteLink(new FullTitle(page)).ToString());
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

			var iconChanges = new SortedList<string, string>(IconNameCache.Count);
			foreach (var kvp in IconNameCache)
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
			var errors = false;
			T? currentSkill = null;
			string? lastName = null; // We use a string for comparison because the skill itself will sometimes massage the data.
			foreach (var row in EsoGeneral.RunQuery(this.Query))
			{
				var currentName = (string)row["skillTypeName"] + "::" + (string)row["baseName"];
				if (lastName != currentName)
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

			// EsoReplacer.ClearReplacementStatus();
			if (SkillSummaryFinder.Matches(page.Text).Count != 1)
			{
				this.Warn("Incorrect number of {{" + TemplateName + "}} matches on " + skill.PageName);
			}

			var match = SkillSummaryFinder.Match(page.Text);
			var template = Template.Parse(match.Value);

			template.RemoveDuplicates();
			template.Remove("update");
			template.NameParameter.After = "\n";
			template.DefaultValueFormat.After = "\n";

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
				iconValue = iconValue.Split(TextArrays.Colon)[0];
			}

			var loopCount = DestructionExceptions.Contains(skill.Name) ? 2 : 0;
			for (var i = 0; i <= loopCount; i++)
			{
				var iconName = "icon" + (i > 0 ? (i + 1).ToStringInvariant() : string.Empty);
				var newValue = IconValueFixup(template[iconName]?.Value, iconValue + (loopCount > 0 ? FormattableString.Invariant($" ({DestructionTypes[i]})") : string.Empty));
				template.AddOrChange(iconName, newValue);
			}

			this.UpdateSkillTemplate(skill, template);
			template.Sort("titlename", "id", "id1", "id2", "id3", "id4", "id5", "id6", "id7", "id8", "id9", "id10", "line", "type", "icon", "icon2", "icon3", "desc", "desc1", "desc2", "desc3", "desc4", "desc5", "desc6", "desc7", "desc8", "desc9", "desc10", "linerank", "cost", "attrib", "casttime", "range", "radius", "duration", "channeltime", "target", "morph1name", "morph1id", "morph1icon", "morph1desc", "morph2name", "morph2id", "morph2icon", "morph2desc", "image", "imgdesc", "nocat", "notrail");

			var newText = template.ToString();
			newText = EsoReplacer.FirstLinksOnly(this.Site, newText);
			page.Text = page.Text.Remove(match.Index, match.Length).Insert(match.Index, newText);

			template = Template.Parse(newText);
			var bigChange = false;
			foreach (var parameter in template)
			{
				if (parameter.Name != null && UpdatedParameters.Contains(parameter.Name))
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