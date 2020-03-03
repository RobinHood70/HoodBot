﻿namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public class LinkFinder : ParsedPageJob
	{
		#region Fields
		private readonly IFullTitle title;
		private readonly TitleCollection results;
		#endregion

		#region Constructors

		[JobInfo("Link Finder")]
		public LinkFinder([ValidatedNotNull] Site site, AsyncInfo asyncInfo, string search)
			: base(site, asyncInfo)
		{
			ThrowNull(search, nameof(search));
			this.Pages.SetLimitations(LimitationType.None);
			this.results = new TitleCollection(site);
			this.title = new FullTitle(site, search);
			this.Logger = null;
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Found links";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			base.Main();
			if (this.results.Count > 0)
			{
				this.results.Sort();
				this.WriteLine($"Pages linking to <nowiki>[[{this.title}]]</nowiki>:");
				foreach (var result in this.results)
				{
					this.WriteLine($"* {result.AsLink(false)}");
				}
			}
		}

		protected override void LoadPages() => this.Pages.GetBacklinks(this.title.FullPageName, BacklinksTypes.Backlinks | BacklinksTypes.ImageUsage, false, Filter.Any);

		protected override void ParseText(object sender, Page page, ContextualParser parsedPage)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(parsedPage, nameof(parsedPage));
			var links = parsedPage.FindAllRecursive<LinkNode>();
			foreach (var link in links)
			{
				var siteLink = SiteLink.FromLinkNode(this.Site, link);
				if (siteLink.FullEquals(this.title))
				{
					this.results.Add(page);
				}
			}
		}
		#endregion
	}
}