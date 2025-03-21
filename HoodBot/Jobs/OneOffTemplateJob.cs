namespace RobinHood70.HoodBot.Jobs;

using System;
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
	protected override string TemplateName => "Flora Summary";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Remove leading 0x in formid";

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		if (template.Find("formid") is IParameterNode formId)
		{
			var value = formId.GetValue();
			if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			{
				formId.SetValue(value[2..], ParameterFormat.Copy);
			}
		}
	}
	#endregion
}