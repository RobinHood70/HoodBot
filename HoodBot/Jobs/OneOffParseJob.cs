namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Parse Job")]
internal sealed class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Renommer les modèles pour plus d'uniformité";

	protected override void LoadPages()
	{
		this.Pages.GetBacklinks("Modèle:Bas de page du livre", BacklinksTypes.EmbeddedIn, true, Filter.Any);
		this.Pages.GetBacklinks("Modèle:Pied de page Livre", BacklinksTypes.EmbeddedIn, true, Filter.Any);
		this.Pages.GetBacklinks("Modèle:Boîte de citations", BacklinksTypes.EmbeddedIn, true, Filter.Any);
		this.Pages.GetBacklinks("Modèle:Arbre", BacklinksTypes.EmbeddedIn, true, Filter.Any);
		this.Pages.GetBacklinks("Modèle:Arbre/début", BacklinksTypes.EmbeddedIn, true, Filter.Any);
		this.Pages.GetBacklinks("Modèle:Arbre/fin", BacklinksTypes.EmbeddedIn, true, Filter.Any);
	}

	protected override void ParseText(SiteParser parser)
	{
		foreach (var template in parser.TemplateNodes)
		{
			var title = template.GetTitle(this.Site);
			if (title.PageNameEquals("Bas de page du livre") || title.PageNameEquals("Pied de page Livre"))
			{
				template.SetTitle("Pied de page de livre");
			}
			else if (title.PageNameEquals("Boîte de citations"))
			{
				template.SetTitle("Boîte de citation");
			}
			else if (title.PageNameEquals("Arbre"))
			{
				template.SetTitle("Chart");
			}
			else if (title.PageNameEquals("Arbre/début"))
			{
				template.SetTitle("Chart/start");
			}
			else if (title.PageNameEquals("Arbre/fin"))
			{
				template.SetTitle("Chart/end");
			}
		}
	}
	#endregion
}