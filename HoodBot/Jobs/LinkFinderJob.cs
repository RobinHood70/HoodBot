namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public abstract class LinkFinderJob : ParsedPageJob
	{
		#region Fields
		private readonly IDictionary<ISimpleTitle, List<string>> results = new SortedDictionary<ISimpleTitle, List<string>>(SimpleTitleComparer.Instance);
		private readonly bool sectionLinksOnly;
		#endregion

		#region Constructors

		protected LinkFinderJob(JobManager jobManager, [JobParameter(DefaultValue = true)] bool sectionLinksOnly)
			: base(jobManager)
		{
			this.Pages.SetLimitations(LimitationType.None);
			this.sectionLinksOnly = sectionLinksOnly;
			this.Logger = null;
			this.Titles = new TitleCollection(this.Site);
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Found links";

		protected TitleCollection Titles { get; }
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			base.Main();
			if (this.results.Count > 0)
			{
				this.WriteLine("{| class=\"wikitable sortable compressed\"");
				foreach (var result in this.results)
				{
					if (result.Value.Count > 0)
					{
						this.WriteLine($"|-\n| {result.Key.AsLink(false)}:");
						this.WriteLine("| " + string.Join("<br>", result.Value));
					}
				}

				this.WriteLine("|}");
			}
		}

		protected override void LoadPages()
		{
			PageCollection pages = PageCollection.Unlimited(this.Site, PageModules.Backlinks, false);
			pages.GetTitles(this.Titles);
			TitleCollection backTitles = new(this.Site);
			foreach (var page in pages)
			{
				foreach (var backlink in page.Backlinks)
				{
					if ((backlink.Value & BacklinksTypes.Backlinks) != 0 || (backlink.Value & BacklinksTypes.ImageUsage) != 0)
					{
						backTitles.Add(backlink.Key);
					}
				}
			}

			this.Pages.GetTitles(backTitles);
		}

		protected void SetTitlesFromSubpages(IEnumerable<ISimpleTitle> titles)
		{
			TitleCollection allTitles = new(this.Site)
			{
				titles.NotNull(nameof(titles))
			};

			foreach (var title in titles)
			{
				allTitles.GetNamespace(title.Namespace.Id, Filter.Exclude, title.PageName + ' ');
			}

			this.Titles.AddRange(allTitles);
		}

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			parsedPage.ThrowNull(nameof(parsedPage));
			foreach (var title in this.Titles)
			{
				if (!this.results.TryGetValue(parsedPage.Context, out var links))
				{
					links = new List<string>();
					this.results.Add(parsedPage.Context, links);
				}

				foreach (var link in parsedPage.FindLinks(title))
				{
					if (this.CheckLink(link) &&
						WikiTextVisitor.Raw(link) is var textTitle &&
						!links.Contains(textTitle, StringComparer.Ordinal))
					{
						links.Add(WikiTextVisitor.Raw(link));
					}
				}
			}
		}

		protected virtual bool CheckLink(SiteLinkNode link) =>
			!(
				this.sectionLinksOnly &&
				SiteLink.FromLinkNode(this.Site, link) is var linkTitle &&
				linkTitle.Fragment == null);
		#endregion
	}
}