namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.Robby;
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
			this.Site.WaitForJobQueue();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateMoves()
		{
			var templatePages = new PageCollection(this.Site);
			templatePages.SetLimitations(LimitationType.OnlyAllow, MediaWikiNamespaces.Template);
			templatePages.GetCategoryMembers("Pages Needing Renaming", CategoryMemberTypes.Page, false);
			foreach (var page in templatePages)
			{
				var parsed = new ContextualParser(page, InclusionType.CurrentPage, false);
				var rename = parsed.FindSiteTemplate("Rename");
				if (rename != null && rename.Find(1) is IParameterNode renameTo)
				{
					this.AddMove(page.FullPageName, renameTo.Value.ToRaw().Replace(" Gods", " Religion", StringComparison.Ordinal));
				}
			}
		}

		//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(LocalConfig.BotDataSubPath("Replacements5.txt"));
		#endregion
	}
}