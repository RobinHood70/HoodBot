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
			this.MoveDelay = 0;
			this.FollowUpActions = FollowUpActions.FixLinks | FollowUpActions.EmitReport | FollowUpActions.CheckLinksRemaining;
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			this.AddReplacement("Blades:Altmer", "Blades:High Elf");
			this.AddReplacement("Blades:Dunmer", "Blades:Dark Elf");
			this.AddReplacement("Blades:Bosmer", "Blades:Wood Elf");
			this.AddReplacement("Morrowind:Altmer", "Morrowind:High Elf");
			this.AddReplacement("Morrowind:Dunmer", "Morrowind:Dark Elf");
			this.AddReplacement("Morrowind:Bosmer", "Morrowind:Wood Elf");
			this.AddReplacement("Oblivion:Altmer", "Oblivion:High Elf");
			this.AddReplacement("Oblivion:Dunmer", "Oblivion:Dark Elf");
			this.AddReplacement("Oblivion:Bosmer", "Oblivion:Wood Elf");
			this.AddReplacement("Skyrim:Altmer", "Skyrim:High Elf");
			this.AddReplacement("Skyrim:Dunmer", "Skyrim:Dark Elf");
			this.AddReplacement("Skyrim:Bosmer", "Skyrim:Wood Elf");
			this.AddReplacement("Online:Altmer", "Online:High Elf");
			this.AddReplacement("Online:Dunmer", "Online:Dark Elf");
			this.AddReplacement("Online:Bosmer", "Online:Wood Elf");
		}
		#endregion
	}
}