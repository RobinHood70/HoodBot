namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;

[method: JobInfo("One-Off Parse Job")]
internal sealed class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Supprimer le modèle PRLA En-tête";

	protected override void LoadPages() => this.Pages.GetBacklinks("Modèle:PRLA En-tête", BacklinksTypes.EmbeddedIn, true, Filter.Exclude);

	protected override void ParseText(SiteParser parser)
	{
		parser.RemoveTemplates("PRLA En-tête");
	}
	#endregion
}