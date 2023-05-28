namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Move Pages")]
		public MovePages(JobManager jobManager, bool updateUserSpace)
				: base(jobManager, updateUserSpace)
		{
			this.MoveAction = MoveAction.MoveSafely;
			this.FollowUpActions = FollowUpActions.Default;
			this.EditSummaryEditMovedPage = "Fix page name case";
			// this.Site.WaitForJobQueue();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateMoves()
		{
			var user = new User(this.Site, "StaticRainstorms");
			var rc = user.GetContributions();
			foreach (var contribution in rc)
			{
				var title = contribution.Title;
				var toName = this.Site.Culture.TextInfo
					.ToTitleCase(title.PageName)
					.Replace(" Of ", " of ", System.StringComparison.Ordinal);
				if (!title.PageNameEquals(toName))
				{
					this.AddReplacement(
						TitleFactory.FromUnvalidated(title.Namespace, title.PageName),
						TitleFactory.FromUnvalidated(title.Namespace, toName),
						ReplacementActions.Move,
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