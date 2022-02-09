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
			this.MoveAction = MoveAction.MoveSafely;
			this.MoveDelay = 500;
			this.EditSummaryMove = "Move to in-game name";
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			TitleCollection titles = new(this.Site);
			titles.GetCategoryMembers("Category:Online-Icons-Abilities-Altmer", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Abilities-Bosmer", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Abilities-Dunmer", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Armor-Altmer", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Armor-Bosmer", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Armor-Dunmer", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Weapons-Altmer", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Weapons-Bosmer", CategoryMemberTypes.File, false);
			titles.GetCategoryMembers("Category:Online-Icons-Weapons-Dunmer", CategoryMemberTypes.File, false);
			foreach (var title in titles)
			{
				var newTitle = title.FullPageName()
					.Replace("Altmer", "High Elf", StringComparison.Ordinal)
					.Replace("Bosmer", "Wood Elf", StringComparison.Ordinal)
					.Replace("Dunmer", "Dark Elf", StringComparison.Ordinal)
					.Replace("Orsimer", "Orc", StringComparison.Ordinal);
				this.AddReplacement(title.FullPageName(), newTitle);
				this.AddReplacement(
					title.FullPageName().Replace("File:", "File talk:", StringComparison.Ordinal),
					newTitle.Replace("File:", "File talk:", StringComparison.Ordinal));
			}

			/*
			titles.GetTitles(
				"Category:Online-Icons-Abilities-Altmer",
				"Category:Online-Icons-Abilities-Bosmer",
				"Category:Online-Icons-Abilities-Dunmer",
				"Category:Online-Icons-Armor-Altmer",
				"Category:Online-Icons-Armor-Bosmer",
				"Category:Online-Icons-Armor-Dunmer",
				"Category:Online-Icons-Weapons-Altmer",
				"Category:Online-Icons-Weapons-Bosmer",
				"Category:Online-Icons-Weapons-Dunmer");
			*/


			//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
			//// this.LoadReplacementsFromFile(@"D:\Data\HoodBot\FileList.txt");
			//// this.AddReplacement("File:ON-item-furnishing-Abecean Ratter Cat.jpg", "File:ON-furnishing-Abecean Ratter Cat.jpg");
		}
		#endregion
	}
}