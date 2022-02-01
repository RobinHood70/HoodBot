namespace RobinHood70.HoodBot.Jobs
{
	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Move Pages")]
		public MovePages(JobManager jobManager)
				: base(jobManager)
		{
			this.DeleteStatusFile();
			this.MoveAction = MoveAction.MoveSafely;
			this.MoveDelay = 500;
			this.EditSummaryMove = "Fix capitalization, per DCSG";
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			this.AddReplacement("Online:Summerset (chapter)", "Online:Summerset (Chapter)");
			this.AddReplacement("Online:Blackwood (chapter)", "Online:Blackwood (Chapter)");
			//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
			//// this.LoadReplacementsFromFile(@"D:\Data\HoodBot\FileList.txt");
			//// this.AddReplacement("File:ON-item-furnishing-Abecean Ratter Cat.jpg", "File:ON-furnishing-Abecean Ratter Cat.jpg");
		}
		#endregion
	}
}