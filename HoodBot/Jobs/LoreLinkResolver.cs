namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
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
		private readonly PageCollection wikiPages;
		#endregion

		#region Constructors
		[JobInfo("LoreLink Resolver")]
		public LoreLinkResolver(JobManager jobManager)
			: base(jobManager)
		{
			this.nsList = new UespNamespaceList(this.Site);
			this.Pages.LoadOptions.Modules |= PageModules.TranscludedIn;
			this.Pages.SetLimitations(LimitationType.Remove, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
			this.wikiPages = new PageCollection(this.Site, PageModules.Info | PageModules.TranscludedIn);
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Update FutureLink/LoreLink";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			this.ResetProgress(UespNamespaces.Gamespaces.Count);
			foreach (var ns in UespNamespaces.Gamespaces)
			{
				this.wikiPages.GetNamespace(ns);
				this.Progress++;
			}
		}

		protected override void LoadPages()
		{
			this.Pages.GetBacklinks("Template:Future Link", BacklinksTypes.EmbeddedIn);
			this.Pages.GetBacklinks("Template:Lore Link", BacklinksTypes.EmbeddedIn);
			var books = new TitleCollection(this.Site);
			books.GetBacklinks("Template:Book Header", BacklinksTypes.EmbeddedIn);
			this.Pages.Remove(books);
		}

		protected override void ParseText(object sender, ContextualParser parser) => parser.Replace(node => this.LinkReplace(node, parser), false);
		#endregion

		#region Private Static Methods
		private HashSet<UespNamespace> GetBacklinkSpaces(Page page)
		{
			var fullSet = new HashSet<UespNamespace>();
			this.RecurseBacklinkSpaces(page, fullSet);
			return fullSet;
		}

		private void RecurseBacklinkSpaces(Page page, HashSet<UespNamespace> fullSet)
		{
			var pageBacklinks = new PageCollection(this.Site);
			foreach (var backlink in page.Backlinks)
			{
				var title = backlink.Key;
				if (backlink.Value == BacklinksTypes.EmbeddedIn &&
					title.Namespace.IsSubjectSpace &&
					title.Namespace != MediaWikiNamespaces.User &&
					this.wikiPages.TryGetValue(title, out var wikiPage) &&
					fullSet.Add(this.nsList.FromTitle(title)))
				{
					// Having fullSet.Add() as a condition avoids circular redirect issues.
					pageBacklinks.Add(wikiPage);
				}
			}

			foreach (var backlinkPage in pageBacklinks)
			{
				this.RecurseBacklinkSpaces(backlinkPage, fullSet);
			}
		}

		private TitleFactory? ResolveLink(params TitleFactory[] titles)
		{
			foreach (var title in titles)
			{
				if (this.wikiPages.Contains(title))
				{
					return title;
				}
			}

			return null;
		}

		private TitleFactory? ResolveTemplate(SiteTemplateNode linkTemplate, UespNamespace ns)
		{
			if (linkTemplate.Find($"{ns.Id}link")?.Value is NodeCollection overridden)
			{
				return TitleFactory.FromUnvalidated(this.Site, overridden.ToRaw());
			}

			if (linkTemplate.Find(1)?.Value is NodeCollection nodes)
			{
				var pageName = nodes.ToRaw();
				var fullName = TitleFactory.FromUnvalidated(this.Site, ns.Full + pageName);
				var parentName = TitleFactory.FromUnvalidated(this.Site, ns.Parent.DecoratedName + pageName);
				var loreName = TitleFactory.FromUnvalidated(this.Site, "Lore:" + pageName);
				var retval = this.ResolveLink(fullName, parentName, loreName);
				var isLoreLink = linkTemplate.TitleValue.PageNameEquals("Lore Link");
				if (retval is null)
				{
					/*
					if (isLoreLink && ns.AssociatedNamespace != UespNamespaces.Lore)
					{
						retval = loreName;
					}
					*/

					if (!isLoreLink && ns.BaseNamespace == UespNamespaces.Lore)
					{
						retval = fullName;
					}
				}

				return retval;
			}

			throw new InvalidOperationException("Template has no valid values.");
		}
		#endregion

		#region Private Methods
		private NodeCollection? LinkReplace(IWikiNode node, ContextualParser parser)
		{
			if (node is not SiteTemplateNode linkTemplate ||
				linkTemplate.TitleValue is not Title callTitle)
			{
				return null;
			}

			// Only assign one to a variable since it's boolean Lore/Future after this.
			var isLoreLink = callTitle.PageNameEquals("Lore Link");
			if (!isLoreLink && !callTitle.PageNameEquals("Future Link"))
			{
				return null;
			}

			var page = parser.Page;
			var linkNode = linkTemplate.Find(1);
			if (linkNode is null)
			{
				throw new InvalidOperationException($"Malformed link node {WikiTextVisitor.Raw(linkTemplate)} on page {page.FullPageName}.");
			}

			var ns = this.GetNamespace(linkTemplate, page);
			var backlinkSpaces = new List<UespNamespace>(this.GetBacklinkSpaces(page));

			// If link doesn't resolve to anything, do nothing.
			if (this.ResolveTemplate(linkTemplate, ns) is not TitleFactory link)
			{
				return null;
			}

			// If transcluded to more than one space or to a space other than the current one, do nothing.
			if (backlinkSpaces.Count > 1 || (backlinkSpaces.Count == 1 && backlinkSpaces[0] != ns))
			{
				return null;
			}

			// If this is a Future Link outside of Lore space that resolves to something IN Lore space, do nothing.
			if (!isLoreLink && link.Namespace == UespNamespaces.Lore &&
				page.Namespace != UespNamespaces.Lore)
			{
				return null;
			}

			var displayText = linkTemplate.PrioritizedFind($"{ns.Id}display", "display", "2") is IParameterNode displayNode
				? displayNode.Value.ToRaw()
				: Title.ToLabelName(linkNode.Value.ToRaw());
			return new NodeCollection(parser.Factory, parser.Factory.LinkNodeFromParts(link.ToString(), displayText));
		}

		private UespNamespace GetNamespace(SiteTemplateNode linkTemplate, Title title)
		{
			if (linkTemplate.Find("ns_base", "ns_id") is IParameterNode nsBase)
			{
				var lookup = nsBase.Value.ToValue();
				return this.nsList.GetAnyBase(lookup)
					?? throw new InvalidOperationException("ns_base invalid in " + WikiTextVisitor.Raw(linkTemplate));
			}

			return this.nsList.FromTitle(title);
		}
		#endregion
	}
}
