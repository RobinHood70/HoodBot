namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.DiffViewers;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class ChangeTest : WikiJob
	{
		[JobInfo("Test Job")]
		public ChangeTest(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}

		protected override void MainJob()
		{
			var titles = new TitleCollection(this.Site, "User:RobinHood70", "User talk:RobinHood70");
			var pages = titles.Load();
			foreach (var page in pages)
			{
				page.Text += "\nHello World!";
				var diffViewer = new VsDiff(page);
				diffViewer.Compare();
				diffViewer.Wait();
			}
		}
	}
}