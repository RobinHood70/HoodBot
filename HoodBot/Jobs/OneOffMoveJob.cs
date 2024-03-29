﻿namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	[method: JobInfo("One-Off Move Job")]
	public class OneOffMoveJob(JobManager jobManager, bool updateUserSpace) : MovePagesJob(jobManager, updateUserSpace)
	{
		#region Protected Override Methods
		protected override void BeforeMain()
		{
			//// this.MoveAction = MoveAction.MoveSafely;
			//// this.SuppressRedirects = false;
			//// this.FollowUpActions = FollowUpActions.FixLinks | FollowUpActions.RetainDirectLinkText;
			this.Site.WaitForJobQueue();
			base.BeforeMain();
		}

		protected override string GetEditSummary(Page page) => "Harmonize Battle Axe spelling";

		protected override void PopulateMoves()
		{
			var titles = new TitleCollection(this.Site);
			titles.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "ON-icon-weapon-Battleaxe");
			foreach (var title in titles)
			{
				var newName = title.FullPageName().Replace("Battleaxe", "Battle Axe", false, this.Site.Culture);
				this.AddMove(title, newName);
			}
		}

		//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(LocalConfig.BotDataSubPath("Replacements5.txt"));
		#endregion
	}
}