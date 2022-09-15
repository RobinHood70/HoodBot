namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Move Pages")]
		public MovePages(JobManager jobManager)
				: base(jobManager)
		{
			this.EditSummaryMove = "Move to new namespace";
			this.MoveAction = MoveAction.None;
			this.FollowUpActions = FollowUpActions.FixLinks | FollowUpActions.EmitReport;
			this.Site.WaitForJobQueue();
		}
		#endregion

		#region Protected Override Methods
		protected override void PageLoaded(EditJob job, Page page)
		{
			// TODO: Nothing to do here. May be a good candidate for a new job type.
		}

		protected override void PopulateMoves()
		{
			var titles = new TitleCollection(this.Site);
			titles.GetNamespace(UespNamespaces.DFUMod, Filter.Any);
			foreach (var title in titles)
			{
				var oldTitle = TitleFactory.FromValidated(this.Site.Namespaces[UespNamespaces.DaggerfallMod], "Daggerfall Unity/Mods/" + title.PageName);
				this.AddMove(oldTitle, title);
			}
		}
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("Replacements5.txt"));
		#endregion
	}
}