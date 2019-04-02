namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class TestJob : WikiJob
	{
		#region Constructors
		[JobInfo("Test Job")]
		public TestJob([ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}
		#endregion

		protected override void Main()
		{
			var links = SiteLink.FindLinks(this.Site, "Some text[[Category:Test]] [[ Image:Example.jpg | x60px | Some text <!-- and a comment {{Template}}--> [[Oblivion:Oblivion]] link ]] More Text", true);
			foreach (var link in links)
			{
				Debug.WriteLine(link.ToString());
				if (link is ImageLink fileLink)
				{
					Debug.WriteLine("Format: " + fileLink.Format);
					Debug.WriteLine("Location: " + fileLink.HorizontalAlignment);
					Debug.WriteLine("Size: " + fileLink.Size);
				}
			}

			var pages = PageCollection.Unlimited(this.Site);
			pages.GetRandom(20);
			foreach (var page in pages)
			{
				Debug.WriteLine("\nPage: " + page.FullPageName);
				links = SiteLink.FindLinks(this.Site, page.Text, true);
				foreach (var link in links)
				{
					Debug.WriteLine(link.ToString());
					if (link is ImageLink fileLink)
					{
						Debug.WriteLine("Format: " + fileLink.Format);
						Debug.WriteLine("Location: " + fileLink.HorizontalAlignment);
						Debug.WriteLine("Size: " + fileLink.Size);
					}
				}
			}
		}
	}
}
