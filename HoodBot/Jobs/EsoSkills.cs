namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Design;
using RobinHood70.HoodBot.Jobs.Design;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

internal sealed class EsoSkills : EditJob
{
	#region Constants
	private const string MinedTable = "minedSkills";
	private const string SkillTable = "skillTree";
	private const string Query =
	"SELECT\n" +
		"st.skillTypeName,\n" +
		"st.learnedLevel,\n" +
		"st.baseName,\n" +
		"st.type,\n" +
		"st.icon,\n" +
		"ms.id,\n" +
		"ms.name,\n" +
		"ms.description,\n" +
		"ms.descHeader,\n" +
		"ms.target,\n" +
		"ms.effectLines,\n" +
		"ms.duration,\n" +
		"ms.cost,\n" +
		"ms.costTime,\n" +
		"ms.maxRange,\n" +
		"ms.minRange,\n" +
		"ms.radius,\n" +
		"ms.isPassive,\n" +
		"ms.castTime,\n" +
		"ms.channelTime,\n" +
		"ms.mechanic,\n" +
		"ms.mechanicTime,\n" +
		"ms.rank,\n" +
		"ms.morph,\n" +
		"ms.coefDescription,\n" +
		"ms.type1, ms.a1, ms.b1, ms.c1, ms.R1,\n" +
		"ms.type2, ms.a2, ms.b2, ms.c2, ms.R2,\n" +
		"ms.type3, ms.a3, ms.b3, ms.c3, ms.R3,\n" +
		"ms.type4, ms.a4, ms.b4, ms.c4, ms.R4,\n" +
		"ms.type5, ms.a5, ms.b5, ms.c5, ms.R5,\n" +
		"ms.type6, ms.a6, ms.b6, ms.c6, ms.R6\n" +
	"FROM\n" +
		"$st st\n" +
	"INNER JOIN\n" +
		"$ms ms ON st.abilityId = ms.id\n" +
	"WHERE\n" +
		/* "st.baseName = 'Lava Whip' AND\n" + */
		"ms.isPlayer AND\n" +
		"ms.morph >= 0 AND\n" +
		"ms.skillLine != 'Emperor'\n" +
	"ORDER BY st.baseName, st.skillTypeName, ms.morph, ms.rank;";
	#endregion

	#region Fields
	private Dictionary<string, Skill> skills = new(StringComparer.Ordinal);
	private EsoVersion? version;
	#endregion

	#region Constructors
	[JobInfo("Skills", "ESO Update")]
	public EsoSkills(JobManager jobManager, bool hideDiffs)
		: base(jobManager)
	{
		jobManager.ShowDiffs = !hideDiffs;
		if (this.Results is PageResultHandler pageResults)
		{
			var title = pageResults.Title;
			pageResults.Title = TitleFactory.FromValidated(title.Namespace, title.PageName + "/ESO Skills");
			pageResults.SaveAsBot = false;
		}

		// TODO: Rewrite Mod Header handling to be more intelligent.
		this.StatusWriteLine("DON'T FORGET TO UPDATE MOD HEADER!");
	}
	#endregion

	#region Public Override Properties
	public override string LogName => "Update ESO Skills";
	#endregion

	#region Protected Override Methods
	protected override void AfterLoadPages() => this.GenerateReport();

	protected override void BeforeLoadPages()
	{
		this.StatusWriteLine("Fetching data");
		UespReplacer.Initialize(this);
		this.version = EsoLog.LatestDBUpdate(false);
		var prevVersion = EsoSpace.GetPatchVersion(this, "botskills");
		if (prevVersion >= this.version)
		{
			prevVersion = new EsoVersion(this.version.Version - 1, false);
		}

		this.skills = GetSkillList(null);
		var prevSkills = GetSkillList(prevVersion);
		foreach (var (key, skill) in this.skills)
		{
			if (prevSkills.TryGetValue(key, out var prevSkill))
			{
				skill.SetChangeType(prevSkill);
			}
		}
	}

	protected override string GetEditSummary(Page page) => this.LogName;

	protected override bool GetIsMinorEdit(Page page) => true;

	protected override void JobCompleted()
	{
		UespReplacer.ShowUnreplaced();
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
		EsoSpace.SetBotUpdateVersion(this, "botskills", this.version!);
	}

	protected override void PageLoaded(Page page)
	{
		var skill = this.skills[page.Title.FullPageName()];
		var oldPage = new SiteParser(page, InclusionType.Transcluded, false);
		var newPage = new SiteParser(page, InclusionType.Transcluded, false);
		skill.UpdatePageText(newPage);
		var replacer = new UespReplacer(this.Site, oldPage, newPage);
		foreach (var warning in replacer.Compare(newPage.Title.FullPageName()))
		{
			this.Warn(warning);
		}
	}
	#endregion

	#region Private Static Methods
	private static SortedList<string, string> GetIconChanges()
	{
		SortedList<string, string> iconChanges = new(Skill.IconNameCache.Count, StringComparer.Ordinal);
		foreach (var kvp in Skill.IconNameCache)
		{
			if (!kvp.Key.OrdinalEquals(kvp.Value))
			{
				iconChanges.Add(kvp.Key, kvp.Value);
			}
		}

		return iconChanges;
	}

	private static Dictionary<string, Skill> GetSkillList(EsoVersion? version)
	{
		Dictionary<string, Skill> retval = new(StringComparer.Ordinal);
		var versionText = version is null ? string.Empty : version.Text;
		var query = Query
			.Replace("$st", SkillTable + versionText, StringComparison.Ordinal)
			.Replace("$ms", MinedTable + versionText, StringComparison.Ordinal);

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
				!currentSkill.PageName.OrdinalEquals(newSkill.PageName))
			{
				currentSkill = newSkill;
				retval.Add(currentSkill.PageName, currentSkill);
			}

			currentSkill.AddData(row);
		}

		foreach (var skill in retval.Values)
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
		List<string> trivialList = [];
		this.WriteLine($"== Skills With Non-Trivial Updates ==");
		foreach (var skill in this.skills)
		{
			var title = (Title)TitleFactory.FromUnvalidated(this.Site, skill.Key);
			if (skill.Value.ChangeType == ChangeType.Major)
			{
				this.WriteLine($"* {{{{Pl|{title.FullPageName()}|{title.PipeTrick()}|diff=cur}}}}");
			}
			else if (skill.Value.ChangeType == ChangeType.Minor)
			{
				trivialList.Add(SiteLink.ToText(title, LinkFormat.LabelName));
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