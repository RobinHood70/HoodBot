namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Parse Job")]
internal sealed class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Standardize mod parameter";

	protected override void LoadPages() => this.Pages.GetNamespace(UespNamespaces.ProjectTamriel, Filter.Any, "Cyrodiil/");

	protected override void ParseText(SiteParser parser)
	{
		foreach (var template in parser.TemplateNodes)
		{
			foreach (var parameter in template.Parameters)
			{
				if (parameter.GetName().OrdinalICEquals("mod"))
				{
					parameter.SetValue("[[Project Tamriel:Cyrodiil/Main Page|Project Cyrodiil]]", ParameterFormat.Copy);
				}
			}
		}
	}
	#endregion
}