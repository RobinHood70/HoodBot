namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.Robby;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

public class OneOffMoveJob : MovePagesJob
{
	#region Fields
	private readonly TitleCollection sortKeyRemoved;
	#endregion

	#region Constructors
	[JobInfo("One-Off Move Job")]
	public OneOffMoveJob(JobManager jobManager)
		: base(jobManager, true, false, true)
	{
		this.sortKeyRemoved = new(this.Site);
		this.MoveAction = MoveAction.None;
		this.FollowUpActions = FollowUpActions.Default | FollowUpActions.UpdateCategoryMembers; // FollowUpActions.CheckLinksRemaining | FollowUpActions.EmitReport | FollowUpActions.FixLinks;
	}
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummaryUpdateLinks(Page page)
	{
		var editSummary = "Modifier le nom de la catégorie";
		if (this.sortKeyRemoved.Contains(page.Title))
		{
			editSummary += "; supprimer la clé de tri obsolète « PAGENAME » (ou similaire)";
		}

		return editSummary;
	}

	protected override void PopulateMoves() => this.AddReplacement("Catégorie:Sous-pages de modèle", "Catégorie:Sous-pages de modèles", JobModels.ReplacementActions.Edit, null);

	protected override void UpdateLinkNode(Page page, LinkNode link, bool isRedirectTarget)
	{
		var title = link.GetTitle(this.Site);
		if (title.Namespace == MediaWikiNamespaces.Category &&
			link.Text.Count == 1 &&
			link.Text[0] is TemplateNode sortTemplate)
		{
			var templateTitle = sortTemplate.GetTitle(this.Site);
			if (templateTitle.PageName.Contains("PAGENAME", System.StringComparison.OrdinalIgnoreCase))
			{
				this.sortKeyRemoved.Add(page.Title);
				link.Text.Clear();
			}
		}

		base.UpdateLinkNode(page, link, isRedirectTarget);
	}
	#endregion
}