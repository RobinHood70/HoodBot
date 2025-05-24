namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using System.IO;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Template Job")]
public class OneOffTemplateJob(JobManager jobManager) : TemplateJob(jobManager)
{
	#region Fields
	private readonly Dictionary<Title, string> ids = [];
	#endregion

	#region Public Override Properties
	public override string LogDetails => "Update " + this.TemplateName;

	public override string LogName => "One-Off Template Job";
	#endregion

	#region Protected Override Properties
	protected override string TemplateName => "Blades Item Summary";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Add/update ID";

	protected override void BeforeLoadPages()
	{
		foreach (var line in File.ReadAllLines(LocalConfig.BotDataSubPath("BladesIds.txt")))
		{
			var split = line.Split('\t');
			this.ids.Add(TitleFactory.FromUnvalidated(this.Site, split[0]), split[1]);
		}

		base.BeforeLoadPages();
	}

	protected override void LoadPages() => this.Pages.GetTitles(this.ids.Keys);

	protected override void ParseTemplate(ITemplateNode template, SiteParser parser)
	{
		template.Update("id", this.ids[parser.Title]);
	}
	#endregion
}