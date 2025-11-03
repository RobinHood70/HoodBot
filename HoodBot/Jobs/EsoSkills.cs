namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	private const string TooltipsTable = "skillTooltips";
	private const string SkillQuery =
		"SELECT\n" +
			"st.abilityId,\n" +
			"st.skillTypeName,\n" +
			"st.learnedLevel,\n" +
			"st.maxRank,\n" +
			"st.baseName,\n" +
			"st.type,\n" +
			"st.icon,\n" +
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
			"ms.rawDescription\n" +
		"FROM\n" +
			"$st st\n" +
		"INNER JOIN\n" +
			"$ms ms ON st.abilityId = ms.id\n" +
		"WHERE\n" +
			/* "st.baseName = 'Reanimate' AND\n" + */
			"ms.isPlayer AND\n" +
			"ms.morph >= 0 AND\n" +
			"ms.skillLine != 'Emperor'\n" +
		"ORDER BY st.baseName, ms.morph, ms.name, ms.rank;";

	private const string CoefficientQuery =
		"SELECT * " +
		"FROM $tt " +
		"ORDER BY abilityId, idx";
	#endregion

	#region Fields
	private Dictionary<string, Skill> skills = new(StringComparer.Ordinal);
	private EsoVersion version;
	#endregion

	#region Constructors
	[JobInfo("Skills", "ESO Update")]
	public EsoSkills(JobManager jobManager, bool hideDiffs)
		: base(jobManager)
	{
		jobManager.ShowDiffs = !hideDiffs;
		var title = TitleFactory.FromUnvalidated(this.Site, jobManager.WikiInfo.ResultsPage + "/ESO Skills");
		this.SetTemporaryResultHandler(new PageResultHandler(title, false));

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
		var dbVersion = EsoLog.LatestDBUpdate("skillTree", false);

		// Version with no extension is one higher than last listed version, so add one if needed.
		this.version = dbVersion.Pts
			? dbVersion
			: new EsoVersion(dbVersion.Version + 1, dbVersion.Pts);
		this.skills = GetSkillList(EsoVersion.Empty);
	}

	protected override string GetEditSummary(Page page) => (page.IsNew ? "Create" : "Update") + " skill";

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
		EsoSpace.SetBotUpdateVersion(this, "botskills", this.version);
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
	private static Dictionary<long, List<Coefficient>> GetCoefficients(EsoVersion version)
	{
		var retval = new Dictionary<long, List<Coefficient>>();
		var versionText = version.ToString();
		var query = CoefficientQuery
			.Replace("$tt", TooltipsTable + versionText, StringComparison.Ordinal);
		foreach (var row in Database.RunQuery(EsoLog.Connection, query))
		{
			var abilityId = (long)(int)row["abilityId"]; // Mismatch in sizes between tables, so convert to long here.
			if (!retval.TryGetValue(abilityId, out var list))
			{
				list = [];
				retval.Add(abilityId, list);
			}

			var coef = new Coefficient(row);
			list.Add(coef);
			if (!coef.IsValid())
			{
				Debug.WriteLine($"Error in abilityId {abilityId}, coef {coef.Index}");
			}
		}

		return retval;
	}

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

	private static Dictionary<string, Skill> GetSkillList(EsoVersion version)
	{
		var coefficients = GetCoefficients(version);
		var versionText = version.ToString();
		var query = SkillQuery
			.Replace("$st", SkillTable + versionText, StringComparison.Ordinal)
			.Replace("$ms", MinedTable + versionText, StringComparison.Ordinal);

		Dictionary<string, Skill> retval = new(StringComparer.Ordinal);
		foreach (var row in Database.RunQuery(EsoLog.Connection, query))
		{
			// As of Update 45, there are a bunch of skills being captured that shouldn't be. All have no prefix in skillTypeName, so check for that and skip the row if that's the case.
			var stName = (string)row["skillTypeName"];
			if (stName.StartsWith("::", StringComparison.Ordinal))
			{
				continue;
			}

			var isPassive = (sbyte)row["isPassive"] == 1;
			Skill newSkill = isPassive
				? new PassiveSkill(row)
				: new ActiveSkill(row);

			// At this point, newSkill has only a few basics and is used primarily to figure out the correct PageName.
			if (!retval.TryGetValue(newSkill.PageName, out var currentSkill))
			{
				retval.Add(newSkill.PageName, newSkill);
				currentSkill = newSkill;
			}

			// Once we've figured out which skill to use for currentSkill, AddData() does the heavy lifting, figuring out which morph and rank we're adding.
			currentSkill.AddData(row, coefficients);
		}

		var errors = false;
		foreach (var skill in retval.Values)
		{
			skill.PostProcess();
			errors |= !skill.IsValid();
		}

		return errors
			? throw new InvalidOperationException("Problems found in skill data.")
			: retval;
	}
	#endregion

	#region Private Methods
	private void GenerateReport()
	{
		var prevVersion = EsoSpace.GetPatchVersion(this, "botskills");
		while (prevVersion >= this.version)
		{
			prevVersion = new EsoVersion(this.version.Version - 1, false);
		}

		var prevSkills = GetSkillList(prevVersion);

		List<string> trivialList = [];
		this.WriteLine($"== Skills With Non-Trivial Updates ==");
		foreach (var (key, skill) in this.skills)
		{
			var changeType = prevSkills.TryGetValue(key, out var prevSkill)
				? skill.GetChangeType(prevSkill)
				: ChangeType.Major;
			var title = TitleFactory.FromUnvalidated(this.Site, key).ToTitle();
			if (changeType.HasFlag(ChangeType.Major))
			{
				this.WriteLine($"* {{{{Pl|{title.FullPageName()}|{title.PipeTrick()}|diff=cur}}}}");
			}
			else if (changeType.HasFlag(ChangeType.Minor))
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