namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Template Job")]
public class OneOffTemplateJob(JobManager jobManager) : TemplateJob(jobManager)
{
	#region Static Fields
	private static readonly HashSet<string> SeasonVariants = new(StringComparer.OrdinalIgnoreCase)
	{
		"2025 Content Pass", "Seasons of the Worm Cult", "Solstice"
	};
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Update " + this.TemplateName + " to use Season of the Worm Cult";

	public override string LogName => "One-Off Template Job";
	#endregion

	#region Protected Override Properties
	protected override string TemplateName => "Mod Header";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Season of the Worm Cult";

	protected override void LoadPages()
	{
		var title = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Template], this.TemplateName);
		this.Pages.GetBacklinks(title.FullPageName(), BacklinksTypes.EmbeddedIn, true, Filter.Exclude, UespNamespaces.Online);
	}

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		foreach (var param in template.Parameters)
		{
			if (param.Anonymous && SeasonVariants.Contains(param.GetValue()))
			{
				param.SetValue("Season of the Worm Cult", ParameterFormat.Copy);
			}
		}
	}
	#endregion
}