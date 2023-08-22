namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class LoreLinkResolver : ParsedPageJob
	{
		#region Fields
		private readonly UespNamespaceList nsList;
		private readonly PageCollection targetPages;
		private readonly PageCollection backlinkPages;

		#endregion

		#region Constructors
		[JobInfo("LoreLink Resolver")]
		public LoreLinkResolver(JobManager jobManager)
			: base(jobManager)
		{
			this.nsList = new UespNamespaceList(this.Site);
			this.backlinkPages = new PageCollection(this.Site, PageModules.Info);
			this.targetPages = new PageCollection(this.Site, PageModules.Info);
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Update LoreLink";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			var limits =
				from ns in this.nsList
				where ns.IsGameSpace && !ns.IsPseudoNamespace
				select ns.BaseNamespace.Id;

			var linkTitles = new TitleCollection(this.Site);
			linkTitles.SetLimitations(LimitationType.OnlyAllow, limits);
			linkTitles.GetBacklinks("Template:Lore Link", BacklinksTypes.EmbeddedIn);
			//// linkTitles.Add("Lore:Ghost");

			// Book Header gets all book variants in one request.
			var books = new TitleCollection(this.Site);
			books.GetBacklinks("Template:Book Header", BacklinksTypes.EmbeddedIn);
			linkTitles.Remove(books);

			var pages = linkTitles.Load(PageModules.Default | PageModules.TranscludedIn);
			this.backlinkPages.AddRange(this.FromTransclusions(pages.Titles()));
			for (var i = pages.Count - 1; i >= 0; i--)
			{
				var page = pages[i];
				if (!this.NamespaceCheck(page, page.Backlinks, new TitleCollection(this.Site)))
				{
					pages.RemoveAt(i);
				}
			}

			this.Pages.AddRange(pages);

			var findTitles = new TitleCollection(this.Site);
			foreach (var page in this.Pages)
			{
				var parser = new ContextualParser(page);
				foreach (var linkTemplate in parser.FindSiteTemplates("Lore Link"))
				{
					var ns = this.GetNamespace(linkTemplate, page);
					string term;
					if (linkTemplate.Find($"{ns.Id}link")?.Value is NodeCollection overridden)
					{
						term = overridden.ToRaw().Trim();
						findTitles.TryAdd(TitleFactory.FromUnvalidated(this.Site, term));
					}
					else if (linkTemplate.Find(1)?.Value is NodeCollection nodes)
					{
						term = nodes.ToRaw().Trim();
						findTitles.TryAdd(TitleFactory.FromUnvalidated(ns.BaseNamespace, term));
						findTitles.TryAdd(TitleFactory.FromUnvalidated(ns.Parent, term));
						findTitles.TryAdd(TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Lore], term));
					}
					else
					{
						term = "<malformed template>";
					}

					Debug.WriteLine($"{term} ({page.Title.FullPageName()})");
				}
			}

			this.targetPages.GetTitles(findTitles);
			//// If the next line is uncommented, it allows altering red links as well
			// this.targetPages.RemoveExists(false);
		}

		protected override void LoadPages()
		{
			foreach (var page in this.Pages)
			{
				this.PageLoaded(page);
			}
		}

		/*
				protected override void Main()
				{
					this.Pages.RemoveChanged(false);
					this.Pages.Sort();
					foreach (var page in this.Pages)
					{
						Debug.WriteLine(page.AsLink());
					}

					base.Main();
				}
		*/

		protected override void ParseText(ContextualParser parser) => parser.Replace(node => this.LinkReplace(node, parser), false);
		#endregion

		#region Private Static Methods
		private Title? ResolveLink(params Title[] titles)
		{
			foreach (var title in titles)
			{
				if (this.targetPages!.Contains(title))
				{
					return title;
				}
			}

			return null;
		}

		private Title? ResolveTemplate(SiteTemplateNode linkTemplate, UespNamespace ns)
		{
			if (linkTemplate.Find($"{ns.Id}link")?.Value is NodeCollection overridden)
			{
				return TitleFactory.FromUnvalidated(this.Site, overridden.ToRaw());
			}

			if (linkTemplate.Find(1)?.Value is NodeCollection nodes)
			{
				var pageName = nodes.ToRaw();
				Title fullName = TitleFactory.FromUnvalidated(ns.BaseNamespace, pageName);
				Title parentName = TitleFactory.FromUnvalidated(ns.Parent, pageName);
				Title loreName = TitleFactory.FromUnvalidated(this.Site[UespNamespaces.Lore], pageName);
				return this.ResolveLink(fullName, parentName, loreName);
			}

			throw new InvalidOperationException("Template has no valid values.");
		}
		#endregion

		#region Private Methods

		private PageCollection FromTransclusions(IEnumerable<Title> titles)
		{
			var fullSet = PageCollection.Unlimited(this.Site, PageModules.Info | PageModules.TranscludedIn, true);
			var nextTitles = new TitleCollection(this.Site, titles);
			do
			{
				Debug.WriteLine($"Loading {nextTitles.Count} transclusion pages.");
				var loadPages = nextTitles.Load(PageModules.Info | PageModules.TranscludedIn);
				fullSet.AddRange(loadPages);
				nextTitles.Clear();
				foreach (var page in loadPages)
				{
					var ns = this.nsList.FromTitle(page.Title);
					foreach (var backlink in page.Backlinks)
					{
						// Once we have a page that's out of the desired namespace, we don't need to follow it anymore, so don't try to load it.
						var title = backlink.Key;
						if (title.Namespace.IsSubjectSpace &&
							title.Namespace == ns.BaseNamespace &&
							!fullSet.Contains(title))
						{
							nextTitles.TryAdd(title);
						}
					}
				}
			}
			while (nextTitles.Count > 0);

			return fullSet;
		}

		private UespNamespace GetNamespace(SiteTemplateNode linkTemplate, ITitle title)
		{
			if (linkTemplate.Find("ns_base", "ns_id") is IParameterNode nsBase)
			{
				var lookup = nsBase.Value.ToValue();
				return this.nsList.GetAnyBase(lookup)
					?? throw new InvalidOperationException("ns_base invalid in " + WikiTextVisitor.Raw(linkTemplate));
			}

			return this.nsList.FromTitle(title.Title);
		}

		private NodeCollection? LinkReplace(IWikiNode node, ContextualParser parser)
		{
			if (node is not SiteTemplateNode linkTemplate ||
				!linkTemplate.TitleValue.PageNameEquals("Lore Link"))
			{
				return null;
			}

			var page = parser.Page;
			var linkNode = linkTemplate.Find(1) ?? throw new InvalidOperationException($"Malformed link node {WikiTextVisitor.Raw(linkTemplate)} on page {page.Title.FullPageName()}.");
			var ns = this.GetNamespace(linkTemplate, page);

			// If link doesn't resolve to anything, do nothing.
			if (this.ResolveTemplate(linkTemplate, ns) is not Title link)
			{
				return null;
			}

			var displayText = linkTemplate.PrioritizedFind($"{ns.Id}display", "display", "2") is IParameterNode displayNode
				? displayNode.Value.ToRaw()
				: Title.ToLabelName(linkNode.Value.ToRaw());
			return new NodeCollection(parser.Factory, parser.Factory.LinkNodeFromParts(link.LinkName(), displayText));
		}

		private bool NamespaceCheck(Page page, IReadOnlyDictionary<Title, BacklinksTypes> backlinks, TitleCollection titlesChecked)
		{
			var ns = this.nsList.FromTitle(page.Title);
			foreach (var backlink in backlinks)
			{
				var title = backlink.Key;
				if (!titlesChecked.Contains(title))
				{
					titlesChecked.Add(title);
					if ((title.Namespace != ns.BaseNamespace && title.Namespace != MediaWikiNamespaces.User) ||
						(this.backlinkPages.TryGetValue(title, out var newBacklinks) &&
						!this.NamespaceCheck(page, newBacklinks.Backlinks, titlesChecked)))
					{
						return false;
					}
				}
			}

			return true;
		}
		#endregion
	}
}