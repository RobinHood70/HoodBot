namespace RobinHood70.HoodBot.Jobs
{
	using System.IO;
	using RobinHood70.WikiCommon;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Move Pages")]
		public MovePages(JobManager jobManager, bool updateUserSpace)
				: base(jobManager)
		{
			if (updateUserSpace)
			{
				this.Pages.SetLimitations(
					Robby.LimitationType.Disallow,
					MediaWikiNamespaces.Media,
					MediaWikiNamespaces.MediaWiki,
					MediaWikiNamespaces.Special,
					MediaWikiNamespaces.Template);
			}

			this.MoveAction = MoveAction.None;
			this.FollowUpActions = FollowUpActions.Default & ~FollowUpActions.EmitReport;
			this.Site.WaitForJobQueue();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateMoves()
		{
			var lines = File.ReadAllLines("D:\\MoveList.txt");
			foreach (var line in lines)
			{
				this.AddMove(line, line.Replace("ON-item-", "ON-", System.StringComparison.Ordinal));
			}
		}

		//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("Replacements5.txt"));
		#endregion
	}
}