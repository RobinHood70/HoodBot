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
	protected override string TemplateName => "Online NPC Summary";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Revert to health=1";

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		if (template.Find("health") is IParameterNode health &&
			string.Equals(health.GetValue(), "{{ESO Health|w}}", System.StringComparison.OrdinalIgnoreCase))
		{
			parser.Page.LoadPreviousRevision();
			var prevParser = new SiteParser(parser.Page, parser.Page.Revisions[1].Text);
			if (prevParser.FindTemplate("Online NPC Summary") is ITemplateNode prevTemplate &&
				prevTemplate.Find("health") is IParameterNode prevHealth &&
				string.Equals(prevHealth.GetValue(), "1", System.StringComparison.Ordinal))
			{
				health.SetValue("1", ParameterFormat.Copy);
			}
		}
	}
	#endregion
}