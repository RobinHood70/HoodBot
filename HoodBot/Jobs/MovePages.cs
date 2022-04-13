namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;

	public class MovePages : MovePagesJob
	{
		#region Fields
		private TitleCollection? furnishingTitles;
		#endregion

		#region Constructors
		[JobInfo("Move Pages")]
		public MovePages(JobManager jobManager)
				: base(jobManager)
		{
			this.DeleteStatusFile();
			this.MoveAction = MoveAction.MoveSafely;
			this.MoveDelay = 500;
			this.EditSummaryMove = "Match page name to item";
		}
		#endregion

		#region Protected Override Methods
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(@"D:\Data\HoodBot\FileList.txt");
		//// this.AddReplacement("File:ON-item-furnishing-Abecean Ratter Cat.jpg", "File:ON-furnishing-Abecean Ratter Cat.jpg");
		protected override void PopulateReplacements()
		{
			/*
			var fileName = Path.Combine(UespSite.GetBotDataFolder("Comma Replacements Oops.txt"));
			var repFile = File.ReadLines(fileName);
			var firstReps = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var line in repFile)
			{
				var rep = line.Split(TextArrays.Tab);
				firstReps.Add(rep[0].Trim(), rep[1].Trim());
			}

			foreach (var rep in firstReps)
			{
				var value = rep.Value;
				while (firstReps.TryGetValue(value, out var rep2))
				{
					value = rep2;
					firstReps.Remove(rep.Key);

				}

				if (!string.Equals(rep.Key, value, StringComparison.Ordinal))
				{
					this.AddReplacement(rep.Key, value);
				}
			}
			*/

			// this.AddReplacement("Online:Bust, The Stonekeeper", "Online:Bust: The Stonekeeper");
			this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("Comma Replacements5.txt"));
		}

		protected override void FilterBacklinkTitles(TitleCollection titles)
		{
			base.FilterBacklinkTitles(titles);
			/*	if (this.furnishingTitles == null)
				{
					this.furnishingTitles = new TitleCollection(this.Site);
					this.furnishingTitles.GetBacklinks("Template:Furnishing Link");
				}

				titles.AddRange(this.furnishingTitles); */
		}
		#endregion
	}
}