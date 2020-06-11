namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class LinkFinder : ParsedPageJob
	{
		#region Fields
		private readonly IFullTitle title;
		private readonly TitleCollection results;
		#endregion

		#region Constructors

		[JobInfo("Link Finder")]
		public LinkFinder([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo, string search)
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

		protected override void LoadPages() => this.Pages.GetBacklinks(this.title.FullPageName(), BacklinksTypes.Backlinks | BacklinksTypes.ImageUsage, false, Filter.Any);

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			ThrowNull(parsedPage, nameof(parsedPage));
			ThrowNull(parsedPage.Title, nameof(parsedPage), nameof(parsedPage.Title));
			var links = parsedPage.FindAllRecursive<LinkNode>();
			foreach (var link in links)
			{
				var siteLink = SiteLink.FromLinkNode(this.Site, link);
				if (siteLink.FullEquals(this.title))
				{
					this.results.Add(parsedPage.Title);
				}
			}
		}
		#endregion
	}
}