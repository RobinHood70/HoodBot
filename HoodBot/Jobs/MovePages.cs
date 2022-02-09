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
			this.FollowUpActions = FollowUpActions.Default | FollowUpActions.UpdateCategoryMembers;
			this.EditSummaryMove = "Move to in-game name";
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			this.AddReplacement("Category:Online-Icons-Abilities-Altmer", "Category:Online-Icons-Abilities-High Elf");
			this.AddReplacement("Category:Online-Icons-Abilities-Bosmer", "Category:Online-Icons-Abilities-Wood Elf");
			this.AddReplacement("Category:Online-Icons-Abilities-Dunmer", "Category:Online-Icons-Abilities-Dark Elf");
			this.AddReplacement("Category:Online-Icons-Armor-Altmer", "Category:Online-Icons-Armor-High Elf");
			this.AddReplacement("Category:Online-Icons-Armor-Bosmer", "Category:Online-Icons-Armor-Wood Elf");
			this.AddReplacement("Category:Online-Icons-Armor-Dunmer", "Category:Online-Icons-Armor-Dark Elf");
			this.AddReplacement("Category:Online-Icons-Weapons-Altmer", "Category:Online-Icons-Weapons-High Elf");
			this.AddReplacement("Category:Online-Icons-Weapons-Bosmer", "Category:Online-Icons-Weapons-Wood Elf");
			this.AddReplacement("Category:Online-Icons-Weapons-Dunmer", "Category:Online-Icons-Weapons-Dark Elf");

			//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
			//// this.LoadReplacementsFromFile(@"D:\Data\HoodBot\FileList.txt");
			//// this.AddReplacement("File:ON-item-furnishing-Abecean Ratter Cat.jpg", "File:ON-furnishing-Abecean Ratter Cat.jpg");
		}
		#endregion
	}
}