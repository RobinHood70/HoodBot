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
			this.MoveAction = MoveAction.None;
			this.MoveDelay = 500;
			this.FollowUpActions = FollowUpActions.FixLinks | FollowUpActions.EmitReport;
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements() =>
			this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(@"D:\Data\HoodBot\FileList.txt");
		//// this.AddReplacement("File:ON-item-furnishing-Abecean Ratter Cat.jpg", "File:ON-furnishing-Abecean Ratter Cat.jpg");
		#endregion
	}
}