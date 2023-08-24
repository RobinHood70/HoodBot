namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;

	public class OneOffMoveJob : MovePagesJob
	{
		#region Constructors
		[JobInfo("One-Off Move Job")]
		public OneOffMoveJob(JobManager jobManager, bool updateUserSpace)
				: base(jobManager, updateUserSpace)
		{
			this.MoveAction = MoveAction.MoveSafely;
			this.SuppressRedirects = false;
			this.FollowUpActions = FollowUpActions.Default;
			this.Site.WaitForJobQueue();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateMoves()
		{
			var fileName = LocalConfig.BotDataSubPath("stars.csv");
			var stars = new CsvFile();
			stars.Load(fileName, true);
			foreach (var star in stars)
			{
				var name = "Starfield:" + star["proper"];
				this.AddMove(name, name + " System");
			}
		}

		//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(LocalConfig.BotDataSubPath("Replacements5.txt"));
		#endregion
	}
}