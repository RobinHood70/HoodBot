namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Template Job")]
public class OneOffTemplateJob(JobManager jobManager) : TemplateJob(jobManager)
{
	#region Public Override Properties
	public override string LogDetails => "Update " + this.TemplateName;

	public override string LogName => "One-Off Template Job";
	#endregion

	#region Protected Override Properties
	protected override string TemplateName => "Effect Link";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Remove unnecessary parameter";

	protected override void ParseTemplate(TemplateNode template, SiteParser parser)
	{
		var pageName = template.Find(1)?.ToRaw();
		var label = template.Find(2)?.ToRaw();
		if (pageName is not null && label is not null)
		{
			var labelName = Title.ToLabelName(pageName);
			if (label.OrdinalEquals(labelName))
			{
				template.Remove("2");
			}
		}
	}
	#endregion
}