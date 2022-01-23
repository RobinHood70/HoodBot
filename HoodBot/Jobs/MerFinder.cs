namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	public class MerFinder : LinkFinderJob
	{
		#region Constructors
		[JobInfo("Mer Finder")]
		public MerFinder(JobManager jobManager)
			: base(jobManager, false)
		{
			TitleCollection titles = new(
				this.Site,
				"Blades:Altmer",
				"Blades:Bosmer",
				"Blades:Dark Elf",
				"Blades:Dunmer",
				"Blades:High Elf",
				"Blades:Wood Elf",
				"Legends:Altmer",
				"Legends:Bosmer",
				"Legends:Dark Elf",
				"Legends:Dunmer",
				"Legends:High Elf",
				"Legends:Wood Elf",
				"Morrowind:Altmer",
				"Morrowind:Bosmer",
				"Morrowind:Dark Elf",
				"Morrowind:Dunmer",
				"Morrowind:High Elf",
				"Morrowind:Wood Elf",
				"Oblivion:Altmer",
				"Oblivion:Bosmer",
				"Oblivion:Dark Elf",
				"Oblivion:Dunmer",
				"Oblivion:High Elf",
				"Oblivion:Wood Elf",
				"Online:Altmer",
				"Online:Bosmer",
				"Online:Dark Elf",
				"Online:Dunmer",
				"Online:High Elf",
				"Online:Wood Elf",
				"Skyrim:Altmer",
				"Skyrim:Bosmer",
				"Skyrim:Dark Elf",
				"Skyrim:Dunmer",
				"Skyrim:High Elf",
				"Skyrim:Wood Elf");
			this.Titles.AddRange(titles);
		}
		#endregion

		protected override bool CheckLink(SiteLinkNode link)
		{
			SiteLink siteLink = SiteLink.FromLinkNode(this.Site, link);
			return siteLink.Text is not "Dark Elf" and not "High Elf" and not "Wood Elf";
		}
	}
}