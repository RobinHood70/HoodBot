﻿namespace RobinHood70.HoodBot.Jobs
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
		private readonly IDictionary<Title, PageLinkList> results = new SortedDictionary<Title, PageLinkList>();
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
			if (this.results.Count == 0)
			{
				return;
			}

			foreach (var section in this.results)
			{
				var linkTitle = this.sectionLinksOnly ? "Section " : string.Empty;
				this.WriteLine($"== {linkTitle}Links to [[{section.Key}]] ==");
				this.WriteLine("{| class=\"wikitable sortable compressed\"");
				this.WriteLine("! Link Found On !! Link");
				foreach (var title in section.Value)
				{
					if (title.Value.Count > 0)
					{
						this.WriteLine($"|-\n| {title.Key.AsLink()}");
						this.Write("| ");
						this.WriteLine(string.Join("<br>", title.Value));
					}
				}

				this.WriteLine("|}");
				this.WriteLine();
			}
		}

		protected override void LoadPages()
		{
			var pages = PageCollection.Unlimited(this.Site, PageModules.Backlinks, false);
			pages.GetTitles(this.Titles);
			TitleCollection backTitles = new(this.Site);
			foreach (var page in pages)
			{
				foreach (var backlink in page.Backlinks)
				{
					if ((backlink.Value & (BacklinksTypes.Backlinks | BacklinksTypes.ImageUsage)) != 0)
					{
						backTitles.Add(backlink.Key);
					}
				}
			}

			this.Pages.GetTitles(backTitles);
		}

		protected void SetTitlesFromSubpages(IEnumerable<Title> titles)
		{
			TitleCollection allTitles = new(this.Site)
			{
				titles.NotNull()
			};

			foreach (var title in titles)
			{
				allTitles.GetNamespace(title.Namespace.Id, Filter.Exclude, title.PageName + ' ');
			}

			this.Titles.AddRange(allTitles);
		}

		protected override void ParseText(object sender, ContextualParser parser)
		{
			parser.ThrowNull();
			foreach (var title in this.Titles)
			{
				if (!this.results.TryGetValue(title, out var section))
				{
					section = new PageLinkList();
					this.results.Add(title, section);
				}

				if (!section.TryGetValue(parser.Page, out var links))
				{
					links = new LinkTargets();
					section.Add(parser.Page, links);
				}

				foreach (var link in parser.FindSiteLinks(title))
				{
					if (this.CheckLink(link) &&
						WikiTextVisitor.Raw(link) is var textTitle &&
						!links.Contains(textTitle, StringComparer.Ordinal))
					{
						links.Add(link.Title.ToRaw().Trim());
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

		#region Private Classes

		//// These are simple wrappers for the relevant classes due to the complex nesting. It also ensures that list styles and such can be changed with a minimum of fuss.

		private sealed class LinkTargets : List<string>
		{
			public LinkTargets()
				: base()
			{
			}
		}

		private sealed class PageLinkList : SortedDictionary<Title, LinkTargets>
		{
			public PageLinkList()
				: base(SimpleTitleComparer.Instance)
			{
			}
		}
		#endregion
	}
}