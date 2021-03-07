namespace RobinHood70.HoodBot.Jobs
{
	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public MovePages(JobManager jobManager)
			: base(jobManager) => this.DeleteStatusFile();
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements() => this.AddReplacement("File:OB-place-market sewers entry Best Defence .jpg", "File:OB-interior-The Best Defense Basement 02.jpg");
		/* {
			// => this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("images_to_be_moved.txt"));
			this.AddReplacement("File:OB-place-Temple Sewers Entrance.jpg", "File:OB-interior-Amantius Allectus' Basement 03.jpg");
			this.AddReplacement("File:OB-upgrade-BattlehornWineCellarAfter.jpg", "File:OB-interior-Battlehorn Castle Wine Cellar.jpg");
			this.AddReplacement("File:OB-upgrade-BattlehornWineCellarBefore.jpg", "File:OB-interior-Battlehorn Castle Wine Cellar 02.jpg");
		} */
		#endregion
	}
}