namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	internal sealed class CoTemplates : WikiJob
	{
		#region Constructors
		[JobInfo("Co-occurring Templates")]
		public CoTemplates(JobManager jobManager, string template1, string template2)
			: base(jobManager, JobType.ReadOnly)
		{
			this.Title1 = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Template], template1);
			this.Title2 = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Template], template2);
		}

		public Title Title1 { get; }

		public Title Title2 { get; }
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var pages = PageCollection.Unlimited(this.Site, PageModules.Templates, true);
			pages.GetBacklinks(this.Title1.FullPageName);
			foreach (var page in pages)
			{
				var titles = new TitleCollection(this.Site, page.Templates);
				if (titles.Contains(this.Title2))
				{
					Debug.WriteLine($"* {page.AsLink(LinkFormat.LabelName)}");
				}
			}
		}
		#endregion
	}
}