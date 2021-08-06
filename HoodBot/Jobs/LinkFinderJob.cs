namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	public abstract class LinkFinderJob : ParsedPageJob
	{
		#region Fields
		private readonly IDictionary<ISimpleTitle, List<string>> results = new SortedDictionary<ISimpleTitle, List<string>>(SimpleTitleComparer.Instance);
		private readonly bool sectionLinksOnly;
		private IEnumerable<ISimpleTitle>? titles;
		#endregion

		#region Constructors

		protected LinkFinderJob(JobManager jobManager, [JobParameter(DefaultValue = true)] bool sectionLinksOnly)
			: base(jobManager)
		{
			this.Pages.SetLimitations(LimitationType.None);
			this.sectionLinksOnly = sectionLinksOnly;
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
				foreach (var result in this.results)
				{
					if (result.Value.Count > 0)
					{
						this.WriteLine($"* {result.Key.AsLink(false)}:");
						foreach (var link in result.Value)
						{
							this.WriteLine($"** {link}");
						}

						this.WriteLine();
					}
				}
			}
		}

		protected override void LoadPages()
		{
			PageCollection? pages = PageCollection.Unlimited(this.Site, PageModules.Backlinks, false);
			pages.GetTitles(this.titles.NotNull(nameof(LinkFinderJob), nameof(this.titles)));
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

			this.titles = allTitles;
		}

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			parsedPage.ThrowNull(nameof(parsedPage));
			foreach (var title in this.titles.NotNull(nameof(LinkFinderJob), nameof(this.titles)))
			{
				if (!this.results.TryGetValue(parsedPage.Context, out var links))
				{
					links = new List<string>();
					this.results.Add(parsedPage.Context, links);
				}

				foreach (var link in parsedPage.FindLinks(title))
				{
					FullTitle? linkTitle = FullTitle.FromBacklinkNode(this.Site, link);
					if (!this.sectionLinksOnly || linkTitle.Fragment != null)
					{
						var textTitle = linkTitle.ToString();
						if (!links.Contains(textTitle, StringComparer.Ordinal))
						{
							links.Add(textTitle);
						}
					}
				}
			}
		}
		#endregion
	}
}