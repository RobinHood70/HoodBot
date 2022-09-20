namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	internal abstract class EsoSkillJob<T> : EditJob
		where T : Skill
	{
		#region Constants
		protected const string SkillTable = "skillTree";
		protected const string MinedTable = "minedSkills";
		#endregion

		#region Fields
		private Dictionary<string, T> skills = new(StringComparer.Ordinal);
		#endregion

		#region Constructors
		protected EsoSkillJob(JobManager jobManager)
			: base(jobManager)
		{
			this.MinorEdit = false;
		}
		#endregion

		#region Public Properties
		public bool BigChange { get; set; }
		#endregion

		#region Public Override Properties
		public override string LogName => "Update ESO " + this.TypeText + " Skills";
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => this.LogName;

		#endregion

		#region Protected Abstract Properties
		protected abstract string Query { get; }

		protected abstract string TypeText { get; }
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
			EsoSpace.SetBotUpdateVersion(this, this.TypeText.ToLowerInvariant());
		}

		protected override void PageLoaded(EditJob job, Page page) =>
			this.skills[page.FullPageName].UpdatePageText(page, this.Site);
		#endregion

		#region Protected Abstract Methods
		protected abstract T NewSkill(IDataRecord row);
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

			var iconChanges = Skill.GetIconChanges();

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
			T? currentSkill = null;
			foreach (var row in Database.RunQuery(EsoLog.Connection, query))
			{
				var newSkill = this.NewSkill(row);

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
	}
}