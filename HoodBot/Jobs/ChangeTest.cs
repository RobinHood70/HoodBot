namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;

	public class ChangeTest : EditJob
	{
		[JobInfo("Test Job")]
		public ChangeTest(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo) => site.EditingDisabled = true;

		public override string LogName { get; } = "Test Job";

		protected override void Main()
		{
			this.ProgressMaximum++;
			var titles = new TitleCollection(this.Site, "User:RobinHood70", "User talk:RobinHood70");
			var pages = titles.Load();
			foreach (var page in pages)
			{
				page.Text += "\nHello World!";
				page.Save("Test", true);
			}

			this.Progress++;
		}
	}
}