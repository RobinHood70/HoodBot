namespace RobinHood70.HoodBot.Jobs;

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
	protected override string TemplateName => "ESO Health";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Update value prior to larger job";

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		if (template.Find(1) is IParameterNode param &&
			string.Equals(param.GetValue(), "dbh", System.StringComparison.OrdinalIgnoreCase))
		{
			param.SetValue("dbl", ParameterFormat.Copy);
		}
	}
	#endregion
}