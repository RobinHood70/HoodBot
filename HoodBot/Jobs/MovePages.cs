namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.IO;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

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
		protected override void PopulateReplacements()
		{
			var fileText = File.ReadAllText(UespSite.GetBotDataFolder("Furnishing Moves.txt"));
			var lines = fileText.Split("~\n", StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var text = line.Split(TextArrays.Tab);
				this.AddReplacement(text[0], "Online:" + text[1]);
			}
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(@"D:\Data\HoodBot\FileList.txt");
		//// this.AddReplacement("File:ON-item-furnishing-Abecean Ratter Cat.jpg", "File:ON-furnishing-Abecean Ratter Cat.jpg");
		}

		protected override void UpdateGalleryLinks(Page page, ITagNode tag) { }

		protected override void UpdateLinkNode(Page page, SiteLinkNode node, bool isRedirectTarget) { }
		#endregion
	}
}