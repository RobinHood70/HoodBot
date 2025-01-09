namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
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
	protected override string TemplateName => "ESO Sets With";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Remove duplicate parameters";

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		var existing = new HashSet<string>(StringComparer.Ordinal);
		for (var i = 0; i < template.Parameters.Count; i++)
		{
			var parameter = template.Parameters[i];
			if (parameter.Anonymous)
			{
				if (!existing.Add(parameter.GetValue()))
				{
					template.Parameters.RemoveAt(i);
				}
			}
		}
	}
	#endregion
}