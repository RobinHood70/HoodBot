namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Move Pages")]
		public MovePages(JobManager jobManager)
				: base(jobManager)
		{
			this.DeleteStatusFile();
			this.MoveAction = MoveAction.None;
			this.FollowUpActions = FollowUpActions.FixLinks;
			this.MoveDelay = 500;
			this.EditSummaryMove = "Move to in-game name";
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			TitleCollection titles = new(this.Site);
			titles.GetCategoryMembers("Category:Online-Icons-Abilities-High Elf", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Abilities-Wood Elf", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Abilities-Dark Elf", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Armor-High Elf", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Armor-Wood Elf", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Armor-Dark Elf", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Weapons-High Elf", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Weapons-Wood Elf", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Weapons-Dark Elf", CategoryMemberTypes.File, false);
			foreach (var title in titles)
			{
				var oldTitle = title.FullPageName()
					.Replace("High Elf", "Altmer", StringComparison.Ordinal)
					.Replace("Wood Elf", "Bosmer", StringComparison.Ordinal)
					.Replace("Dark Elf", "Dunmer", StringComparison.Ordinal);
				this.AddReplacement(oldTitle, title.FullPageName());
			}

			//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
			//// this.LoadReplacementsFromFile(@"D:\Data\HoodBot\FileList.txt");
			//// this.AddReplacement("File:ON-item-furnishing-Abecean Ratter Cat.jpg", "File:ON-furnishing-Abecean Ratter Cat.jpg");
		}
		#endregion
	}
}