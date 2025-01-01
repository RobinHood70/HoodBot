namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Move Job")]
public class OneOffMoveJob(JobManager jobManager, bool updateUserSpace) : MovePagesJob(jobManager, updateUserSpace)
{
	#region Protected Override Methods
	protected override bool BeforeMain()
	{
		this.MoveAction = MoveAction.None;
		this.ParameterReplacers.AddTemplateReplacers("PC3", this.StirkReplace);
		return base.BeforeMain();
	}


	protected void StirkReplace(Page page, ITemplateNode template)
	{
		if (template.Find(1) is not IParameterNode param1)
		{
			return;
		}

		if (!param1.GetValue().Replace('_', ' ').OrdinalEquals("Stirk (city)"))
		{
			return;
		}

		param1.SetValue("Charach", ParameterFormat.Copy);
		this.ParameterReplacers.PageNameReplace(page.Title.Namespace, param1);
		if (template.Find(2) is not IParameterNode param2)
		{
			return;
		}

		var param2Value = param2
			.GetValue()
			.Replace("Sitrk", "Charach", StringComparison.OrdinalIgnoreCase)
			.Replace("Stirk", "Charach", StringComparison.OrdinalIgnoreCase);

		if (string.Compare(param1.GetValue(), param2Value, false, this.Site.Culture) == 0)
		{
			template.Remove("2");
		}
		else
		{
			param2.SetValue(param2Value, ParameterFormat.Copy);
		}
	}


	protected override string GetEditSummary(Page page) => "Link past redirect";

	protected override void PopulateMoves()
	{
		this.AddLinkUpdate("Project Tamriel:Cyrodiil/Stirk (city)", "Project Tamriel:Cyrodiil/Charach");
	}

	//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
	//// this.AddMove(title, newName);
	//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
	//// this.LoadReplacementsFromFile(LocalConfig.BotDataSubPath("Replacements5.txt"));
	protected override void UpdateLinkText(ITitle page, SiteLink from, SiteLink toLink, bool addCaption)
	{
		base.UpdateLinkText(page, from, toLink, addCaption);
		if (from.Text.OrdinalICEquals("Stirk") ||
			from.Text.OrdinalICEquals("Stirk (city)"))
		{
			toLink.Text = "Charach";
		}
	}
	#endregion
}