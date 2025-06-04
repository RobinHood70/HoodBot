namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
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
	public override string LogDetails => "Update " + this.TemplateName;

	public override string LogName => "One-Off Template Job";
	#endregion

	#region Protected Override Properties
	protected override string TemplateName => "ESO DLC";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update template name";

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		if (string.Equals(template.Find(1)?.GetValue(), "Seasons of the Worm Cult", StringComparison.OrdinalIgnoreCase))
		{
			template.SetTitle("ESO S-WC");
			template.Parameters.Clear();
		}
	}
	#endregion
}