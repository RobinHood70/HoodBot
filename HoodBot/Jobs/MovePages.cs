namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Move Pages")]
		public MovePages(JobManager jobManager, bool updateUserSpace)
				: base(jobManager, updateUserSpace)
		{
			this.MoveAction = MoveAction.MoveSafely;
			this.FollowUpActions = FollowUpActions.Default;
			this.EditSummaryEditMovedPage = "Remove Rename template";
			this.Site.WaitForJobQueue();
		}
		#endregion

		#region Protected Override Methods
		protected override void CustomEdit(ContextualParser parser, Title from)
		{
			parser.RemoveAll<SiteTemplateNode>(template => template.TitleValue.Namespace == MediaWikiNamespaces.Template && template.TitleValue.PageNameEquals("Rename"));
			if (parser.Count > 1)
			{
				for (var i = parser.Count - 2; i >= 0; i--)
				{
					if (
						parser[i] is IIgnoreNode ignoreOpen && string.Equals(ignoreOpen.Value, "<noinclude>", StringComparison.Ordinal) &&
						parser[i + 1] is IIgnoreNode ignoreClose && string.Equals(ignoreClose.Value, "</noinclude>", StringComparison.Ordinal))
					{
						parser.RemoveAt(i + 1);
						parser.RemoveAt(i);
					}
				}
			}
		}

		protected override void PopulateMoves()
		{
			var templatePages = new PageCollection(this.Site);
			templatePages.SetLimitations(LimitationType.OnlyAllow, MediaWikiNamespaces.Template);
			templatePages.GetCategoryMembers("Pages Needing Renaming", CategoryMemberTypes.Page, false);
			foreach (var page in templatePages)
			{
				var parsed = new ContextualParser(page, InclusionType.CurrentPage, false);
				var rename = parsed.FindSiteTemplate("Rename");
				if (rename != null && rename.Find(1) is IParameterNode renameParam)
				{
					var renameTo = renameParam.Value.ToRaw().Replace(" Gods", " Religion", StringComparison.Ordinal);
					this.AddReplacement(
						TitleFactory.FromUnvalidated(this.Site, page.FullPageName),
						TitleFactory.FromUnvalidated(this.Site, renameTo),
						ReplacementActions.Move | ReplacementActions.Edit,
						null);
				}
			}
		}

		//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(LocalConfig.BotDataSubPath("Replacements5.txt"));
		#endregion
	}
}