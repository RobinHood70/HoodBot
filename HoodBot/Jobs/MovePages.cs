namespace RobinHood70.HoodBot.Jobs
{
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

			this.MoveAction = MoveAction.MoveSafely;
			this.FollowUpActions = FollowUpActions.Default;
			this.Site.WaitForJobQueue();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateMoves()
		{
			this.AddMove("Online:Frostbane Bear Mount", "Online:Frostbane Bear (mount)");
			this.AddMove("Online:Frostbane Bear Pet", "Online:Frostbane Bear (pet)");
			this.AddMove("Online:Frostbane Sabre Cat Pet", "Online:Frostbane Sabre Cat (pet)");
			this.AddMove("Online:Frostbane Sabre Cat Mount", "Online:Frostbane Sabre Cat (mount)");
			this.AddMove("Online:Frostbane Wolf Mount", "Online:Frostbane Wolf (mount)");
			this.AddMove("Online:Frostbane Wolf Pet", "Online:Frostbane Wolf (pet)");
		}

		//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("Replacements5.txt"));
		#endregion
	}
}