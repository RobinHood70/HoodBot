namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	#region Public Enums
	[Flags]
	public enum FollowUpActions
	{
		None = 0,
		CheckLinksRemaining = 1,
		EmitReport = 1 << 1,
		FixLinks = 1 << 2,
		UpdateCaption = 1 << 3,
		ProposeUnused = 1 << 4,
		UpdateCategoryMembers = 1 << 5,
		UpdatePageNameCaption = 1 << 6,
		AffectsBacklinks = FixLinks | ProposeUnused | UpdateCategoryMembers,
		NeedsCategoryMembers = ProposeUnused | UpdateCategoryMembers,
		Default = CheckLinksRemaining | EmitReport | FixLinks,
	}

	public enum MoveAction
	{
		None,
		MoveSafely,
		MoveOverExisting,
	}

	[Flags]
	public enum MoveOptions
	{
		None = 0,
		MoveSubPages = 1 << 1,
		MoveTalkPage = 1 << 2
	}
	#endregion

	// TODO: Split out ProposeUnused code into a separate job; have this one only check, not change.

	/// <summary>Underlying job to move pages. This partial class contains the job logic, while the other one contains the parameter replacer methods.</summary>
	/// <seealso cref="EditJob" />
	public abstract class MovePagesJob : EditJob
	{
		#region Fields
		private readonly IDictionary<Title, DetailedActions> actions;
		private readonly ParameterReplacers parameterReplacers;
		private readonly IDictionary<Title, Title> moves;
		private readonly IDictionary<Title, Title> linkUpdates;
		private bool isRedirectLink;
		private string? logDetails;
		#endregion

		#region Constructors
		protected MovePagesJob(JobManager jobManager)
			: base(jobManager)
		{
			this.linkUpdates = new Dictionary<Title, Title>(SimpleTitleComparer.Instance);

			this.actions = new Dictionary<Title, DetailedActions>(SimpleTitleComparer.Instance);
			this.moves = new Dictionary<Title, Title>(SimpleTitleComparer.Instance);

			this.parameterReplacers = new ParameterReplacers(jobManager.Site, this.LinkUpdates);
		}
		#endregion

		#region Public Override Properties

		public override string LogDetails
		{
			get
			{
				if (this.logDetails == null)
				{
					List<string> list = new();
					if (this.MoveAction == MoveAction.MoveOverExisting)
					{
						list.Add("move pages over existing pages");
					}
					else if (this.MoveAction == MoveAction.MoveSafely)
					{
						list.Add("move pages");
					}

					if (this.MoveAction != MoveAction.None)
					{
						var value = (this.SuppressRedirects ? "suppress" : "create") + "redirects";
						list.Add(value);
					}

					if ((this.FollowUpActions & FollowUpActions.FixLinks) != 0)
					{
						list.Add("fix links");
					}

					if ((this.FollowUpActions & FollowUpActions.UpdateCategoryMembers) != 0)
					{
						list.Add("re-categorize category members");
					}

					if ((this.FollowUpActions & FollowUpActions.ProposeUnused) != 0)
					{
						list.Add("propose unused pages for deletion");
					}

					if ((this.FollowUpActions & FollowUpActions.CheckLinksRemaining) != 0)
					{
						list.Add("report remaining links");
					}

					list[0] = list[0].UpperFirst(this.Site.Culture);
					this.logDetails = string.Join(", ", list);
				}

				return this.logDetails;
			}
		}

		public override string LogName => "Move Pages";
		#endregion

		#region Protected Properties
		protected bool AllowFromEqualsTo { get; set; }

		protected bool DeleteOnSuccess { get; set; } = true;

		protected string EditSummaryEditMovedPage { get; set; } = "Update moved page text";

		protected string EditSummaryMove { get; set; } = "Rename";

		protected string EditSummaryPropose { get; set; } = "Propose for deletion";

		protected string EditSummaryUpdateLinks { get; set; } = "Update links after page move";

		protected FollowUpActions FollowUpActions { get; set; } = FollowUpActions.Default;

		protected IReadOnlyDictionary<Title, Title> LinkUpdates => (IReadOnlyDictionary<Title, Title>)this.linkUpdates;

		protected MoveAction MoveAction { get; set; } = MoveAction.MoveSafely;

		protected MoveOptions MoveExtra { get; set; }

		protected PageModules PageInfoExtraModules { get; set; }

		protected bool RecursiveCategoryMembers { get; set; } = true;

		protected bool SuppressRedirects { get; set; } = true;
		#endregion

		#region Protected Methods

		protected void AddLinkUpdate(string from, string to) => this.AddLinkUpdate(
			TitleFactory.FromUnvalidated(this.Site, from),
			TitleFactory.FromUnvalidated(this.Site, to));

		protected void AddLinkUpdate(Title from, Title to) => this.AddReplacement(from, to, ReplacementActions.None, null);

		protected void AddMove(string from, string to) => this.AddMove(
			TitleFactory.FromUnvalidated(this.Site, from),
			TitleFactory.FromUnvalidated(this.Site, to));

		protected void AddMove(Title from, Title to) => this.AddReplacement(from, to, ReplacementActions.Move, null);

		protected void AddReplacement(Title from, Title to, ReplacementActions initialActions, string? reason)
		{
			if (!this.AllowFromEqualsTo && from.SimpleEquals(to))
			{
				throw new InvalidOperationException($"From and To titles cannot be the same: {from}");
			}

			if (this.MoveAction != MoveAction.None)
			{
				this.moves.Add(from, to);
			}

			if (this.MoveAction == MoveAction.None ||
				initialActions == ReplacementActions.None ||
				(this.FollowUpActions & FollowUpActions.FixLinks) != 0)
			{
				this.linkUpdates.Add(from, to);
			}

			this.actions.Add(from, new DetailedActions(initialActions, reason));
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Getting Replacement List");
			this.PopulateMoves();
			if (this.MoveAction != MoveAction.None)
			{
				this.ValidateMoves();
			}

			var fromPages = this.GetFromPages();
			this.ValidateMoveActions(fromPages);
			var categoryMembers = this.GetCategoryMembers(fromPages);
			var loadTitles = this.GetLoadTitles(fromPages, categoryMembers);

			this.StatusWriteLine("Figuring out what to do");
			if ((this.FollowUpActions & FollowUpActions.ProposeUnused) != 0)
			{
				this.SetupProposedDeletions(fromPages, categoryMembers);
			}

			if (this.MoveAction != MoveAction.None)
			{
				this.ValidateActions();
			}

			if ((this.FollowUpActions & FollowUpActions.EmitReport) != 0)
			{
				this.EmitReport();
				this.Results?.Save();
			}

			this.Pages.PageLoaded += this.FullEdit;
			this.Pages.GetTitles(loadTitles);
			this.Pages.PageLoaded -= this.FullEdit;
		}

		protected override void Main()
		{
			this.SavePages(this.EditSummaryUpdateLinks, true, this.FullEdit);
			if (this.MoveAction != MoveAction.None)
			{
				this.MovePages();
			}

			if ((this.FollowUpActions & FollowUpActions.CheckLinksRemaining) != 0)
			{
				this.CheckRemaining();
			}
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void PopulateMoves();
		#endregion

		#region Protected Virtual Methods
		protected virtual void BacklinkPageLoaded(ContextualParser parser) => this.ReplaceBacklinks(parser.NotNull().Page, parser);

		protected virtual void CheckRemaining()
		{
			this.StatusWriteLine("Checking remaining pages");
			TitleCollection leftovers = new(this.Site);
			var allBacklinks = PageCollection.Unlimited(this.Site, PageModules.Info | PageModules.Backlinks, false);
			TitleCollection backlinkTitles = new(this.Site);
			foreach (var replacement in this.moves)
			{
				backlinkTitles.Add(replacement.Key);
			}

			allBacklinks.GetTitles(backlinkTitles);
			foreach (var page in allBacklinks)
			{
				foreach (var backlink in page.Backlinks)
				{
					if (!page.Equals(backlink.Key))
					{
						leftovers.Add(page);
					}
				}
			}

			if (leftovers.Count > 0)
			{
				leftovers.Sort();
				this.WriteLine("The following pages are still linked to:");
				foreach (var title in leftovers)
				{
					this.WriteLine($"* [[Special:WhatLinksHere/{title}|{title}]]");
				}
			}
		}

		protected virtual void CustomEdit(Page page, Title from)
		{
		}

		protected virtual void EditPageLoaded(ContextualParser parser, Title from)
		{
			var page = parser.Page;
			var action = this.actions[from];
			if (page.Exists &&
				(this.FollowUpActions & FollowUpActions.ProposeUnused) != 0 &&
				action.HasAction(ReplacementActions.Propose))
			{
				var reason = action.Reason.NotNull();
				ProposeForDeletion(parser, "{{Proposeddeletion|bot=1|" + reason.UpperFirst(this.Site.Culture) + "}}");
				this.SetSaveInfoForPage(from, this.EditSummaryPropose, false);
			}

			if (action.HasAction(ReplacementActions.Edit))
			{
				this.CustomEdit(page, from);
			}
		}

		protected virtual void EmitReport()
		{
			this.WriteLine("{| class=\"wikitable sortable\"");
			this.WriteLine("! Title !! Action");
			foreach (var (from, action) in this.actions)
			{
				this.WriteLine("|-");
				this.Write(FormattableString.Invariant($"| {from} ([[Special:WhatLinksHere/{from}|links]]) || "));
				List<string> actionsList = new();
				if (action.HasAction(ReplacementActions.Move))
				{
					actionsList.Add("move to " + this.moves[from].AsLink());
					if (this.FollowUpActions.HasFlag(FollowUpActions.FixLinks))
					{
						actionsList.Add("update links");
					}
				}
				else
				{
					if (this.FollowUpActions.HasFlag(FollowUpActions.FixLinks))
					{
						actionsList.Add("update links to " + this.linkUpdates[from].AsLink());
					}
				}

				if (action.HasAction(ReplacementActions.Edit))
				{
					actionsList.Add("edit" + (action.HasAction(ReplacementActions.Move) ? " moved page" : string.Empty));
				}

				if (action.HasAction(ReplacementActions.Propose))
				{
					actionsList.Add("propose for deletion");
				}

				if (action.HasAction(ReplacementActions.Skip))
				{
					actionsList.Add("skip");
				}

				if (actionsList.Count == 0)
				{
					actionsList.Add("none");
				}

				var actionText = string.Join(", ", actionsList).UpperFirst(this.Site.Culture);
				if (action.Reason != null)
				{
					actionText += " (" + action.Reason + ")";
				}

				this.WriteLine(actionText);
			}

			this.WriteLine("|}");
		}

		protected virtual void FilterBacklinkTitles(TitleCollection titles)
		{
			titles.RemoveNamespaces(
				MediaWikiNamespaces.Media,
				MediaWikiNamespaces.MediaWiki,
				MediaWikiNamespaces.Special);

			foreach (var title in this.Site.DiscussionPages)
			{
				titles.Remove(title);
			}

			foreach (var title in this.Site.FilterPages)
			{
				titles.Remove(title);
			}

			FilterTemplatesExceptDocs(titles);
		}

		protected virtual IReadOnlyDictionary<Title, TitleCollection> GetCategoryMembers(PageCollection fromPages)
		{
			this.StatusWriteLine("Getting category members");
			Dictionary<Title, TitleCollection> retval = new();
			if ((this.FollowUpActions & FollowUpActions.NeedsCategoryMembers) != 0)
			{
				var categoryReplacements = new Dictionary<Title, Title>();
				foreach (var replacement in this.linkUpdates)
				{
					if (replacement.Key.Namespace == MediaWikiNamespaces.Category &&
						replacement.Value.Namespace == MediaWikiNamespaces.Category)
					{
						categoryReplacements.Add(replacement.Key, replacement.Value);
					}
				}

				if (categoryReplacements.Count == 0)
				{
					return ImmutableDictionary<Title, TitleCollection>.Empty;
				}

				this.ResetProgress(categoryReplacements.Count);
				foreach (var category in categoryReplacements)
				{
					var from = category.Key;
					if ((this.FollowUpActions & FollowUpActions.ProposeUnused) != 0 ||
						this.linkUpdates.ContainsKey(from))
					{
						TitleCollection catMembers = new(this.Site);
						catMembers.GetCategoryMembers(from.PageName, CategoryMemberTypes.All, this.RecursiveCategoryMembers);
						if (catMembers.Count > 0)
						{
							retval.Add(from, catMembers);
						}

						this.Progress++;
					}
				}
			}

			return retval;
		}

		protected virtual DetailedActions HandleConflict(Title from, Title to)
		{
			var action = this.actions[from];
			return new(action.Actions & ~ReplacementActions.Move, $"{to.AsLink()} exists");
		}

		protected virtual Title? LinkUpdateMatch(SiteLink from) =>
			this.linkUpdates.TryGetValue(from, out var to) &&
			(from.ForcedNamespaceLink
				|| from.Namespace != MediaWikiNamespaces.Category
				|| to.Namespace != from.Namespace
				|| (this.FollowUpActions & FollowUpActions.UpdateCategoryMembers) != 0)
			? to
			: null;

		protected virtual void MovePages()
		{
			this.StatusWriteLine("Moving pages");
			this.Progress = 0;
			var moveTitles = new List<Title>();
			foreach (var action in this.actions)
			{
				if (action.Value.HasAction(ReplacementActions.Move))
				{
					moveTitles.Add(action.Key);
				}
			}

			moveTitles.Sort(SimpleTitleComparer.Instance);

			this.ProgressMaximum = moveTitles.Count;
			foreach (var from in moveTitles)
			{
				var to = this.moves[from];
				var fromNs = from.Namespace;
				var moveTalkPage =
					!fromNs.IsTalkSpace &&
					(this.MoveExtra & MoveOptions.MoveTalkPage) != 0;
				var moveSubPages =
					fromNs.AllowsSubpages &&
					(this.MoveExtra & MoveOptions.MoveSubPages) != 0;
				this.Site.Move(
					from,
					to,
					this.EditSummaryMove,
					moveTalkPage,
					moveSubPages,
					this.SuppressRedirects);
				this.Progress++;
			}
		}

		protected virtual void ReplaceBacklinks(Page page, NodeCollection nodes)
		{
			page.ThrowNull();
			nodes.ThrowNull();

			// Possibly better as a visitor class?
			this.isRedirectLink = page.IsRedirect;
			foreach (var node in nodes)
			{
				if (node is IParentNode parent)
				{
					foreach (var subCollection in parent.NodeCollections)
					{
						this.ReplaceBacklinks(page, subCollection);
					}
				}

				this.ReplaceSingleNode(page, node);
			}
		}

		protected virtual void ReplaceSingleNode(Page page, IWikiNode node)
		{
			switch (node)
			{
				case ITagNode tag:
					if (string.Equals(tag.Name, "gallery", StringComparison.Ordinal))
					{
						this.UpdateGalleryLinks(page, tag);
					}

					break;
				case ILinkNode link:
					this.UpdateLinkNode(page, link, this.isRedirectLink);
					this.isRedirectLink = false;
					break;
				case SiteTemplateNode template:
					this.UpdateTemplateNode(page, template);
					break;
			}
		}

		protected virtual void SetupProposedDeletions(PageCollection fromPages, IReadOnlyDictionary<Title, TitleCollection> catMembers)
		{
			var deletions = this.LoadProposedDeletions();
			TitleCollection doNotDelete = new(this.Site);
			foreach (var template in this.Site.DeletePreventionTemplates)
			{
				doNotDelete.GetBacklinks(template.FullPageName, BacklinksTypes.EmbeddedIn, true);
			}

			foreach (var linkUpdate in this.linkUpdates)
			{
				var fromPage = fromPages[linkUpdate.Key];
				if (fromPage.Exists &&
					fromPage.Backlinks.Count == 0 &&
					!catMembers.ContainsKey(fromPage))
				{
					var action = this.actions[linkUpdate.Key];
					if (doNotDelete.Contains(fromPage))
					{
						if (this.MoveAction != MoveAction.None)
						{
							action.SetMoveActions(ReplacementActions.Skip, "no links, but marked to not be deleted");
						}
					}
					else if (deletions.Contains(fromPage))
					{
						action.SetMoveActions(ReplacementActions.Skip, "no links, but already proposed for deletion");
					}
					else
					{
						action.SetMoveActions(ReplacementActions.Propose, "unused so propose for deletion");
					}
				}
			}
		}

		protected virtual void UpdateGalleryLinks(Page page, ITagNode tag)
		{
			if (tag is null || tag.InnerText is not string text || text.Trim().Length == 0)
			{
				return;
			}

			StringBuilder sb = new();
			var lines = text.Split(TextArrays.LineFeed);
			foreach (var line in lines)
			{
				var trimmedLine = line.Trim();
				if (trimmedLine.Length > 0)
				{
					try
					{
						trimmedLine = this.UpdateGalleryLink(page, trimmedLine);
					}
					catch (ArgumentException)
					{
						this.StatusWriteLine($"Malformed gallery link on {page.FullPageName} in line {line}");
					}
				}

				sb
					.Append(trimmedLine)
					.Append('\n');
			}

			if (sb.Length > 0)
			{
				sb.Remove(sb.Length - 1, 1);
			}

			tag.InnerText = sb.ToString();
		}

		protected virtual SiteLink GetToLink(Title page, bool isRedirectTarget, SiteLink from, Title to)
		{
			var retval = from.WithTitle(to);
			if (this.GetLinkText(page, from, retval, !isRedirectTarget) is string linkText)
			{
				retval.Text = linkText;
			}

			return retval;
		}

		protected virtual void UpdateLinkNode(Page page, ILinkNode node, bool isRedirectTarget)
		{
			page.ThrowNull();
			node.ThrowNull();
			var from = SiteLink.FromLinkNode(this.Site, node);
			if (this.LinkUpdateMatch(from) is Title to)
			{
				this
					.GetToLink(page, isRedirectTarget, from, to)
					.UpdateLinkNode(node);
			}

			if (from.Namespace == MediaWikiNamespaces.Media)
			{
				Title key = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.File], from.PageName);
				if (this.linkUpdates.TryGetValue(key, out var toMedia))
				{
					this
						.GetToLink(page, isRedirectTarget, from, TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Media], toMedia!.PageName))
						.UpdateLinkNode(node);
				}
			}
		}

		protected virtual string? GetLinkText(Title page, Title from, SiteLink toLink, bool addCaption)
		{
			page.ThrowNull();
			from.ThrowNull();
			toLink.ThrowNull();
			if ((this.FollowUpActions & FollowUpActions.UpdateCaption) != 0)
			{
				// If UpdateCaption is true and caption exactly matches either the full page name or the simple page name, update it.
				if (toLink.Text != null
					&& page.Namespace != MediaWikiNamespaces.User
					&& !page.Site.IsDiscussionPage(page))
				{
					var newTitle = TitleFactory.FromUnvalidated(this.Site[MediaWikiNamespaces.Main], toLink.Text);
					var to = this.linkUpdates[from];
					if (from is IFullTitle fullTitle)
					{
						return fullTitle.FullEquals(newTitle) ? to.ToString() : null;
					}

					if (from.SimpleEquals(newTitle))
					{
						return to.ToString();
					}

					if ((this.FollowUpActions & FollowUpActions.UpdatePageNameCaption) != 0)
					{
						if (string.Equals(from.FullPageName, toLink.Text, StringComparison.Ordinal))
						{
							return to.FullPageName;
						}

						if (string.Equals(from.PageName, toLink.Text, StringComparison.Ordinal))
						{
							return to.PageName;
						}
					}
				}
			}
			else if (addCaption &&
				toLink.Text == null &&
				toLink.OriginalTitle != null &&
				(toLink.ForcedNamespaceLink || !toLink.Namespace.IsForcedLinkSpace))
			{
				// if UpdateCaption is false, then we want to preserve the previous display text for any caption-less links by adding a new caption. Logic further up sets addCaption appropriately so this won't occur for a redirect target or in galleries.
				return toLink.OriginalTitle.TrimStart(':');
			}

			return null;
		}

		protected virtual void UpdateTemplateNode(Page page, SiteTemplateNode template)
		{
			page.ThrowNull();
			template.ThrowNull();
			var fullTitle = new FullTitle(template.TitleValue);
			if (this.linkUpdates.TryGetValue(fullTitle, out var to))
			{
				var nameText = to.Namespace == MediaWikiNamespaces.Template
					? to.PageName
					: to.Namespace.DecoratedName + to.PageName;
				template.SetTitle(nameText);
			}

			this.parameterReplacers.ReplaceAll(page, template);
		}

		protected virtual void ValidateMoveActions(PageCollection fromPages)
		{
			var toPages = this.GetToPages();
			foreach (var move in this.moves)
			{
				var action = this.actions[move.Key];
				if (action.HasAction(ReplacementActions.Move) && !action.HasAction(ReplacementActions.Skip))
				{
					var fromPage = fromPages[move.Key];
					if (!fromPage.Exists)
					{
						// From title does not exist. Should still have an entry in linkUpdates, if appropriate.
						this.actions[move.Key] = new(ReplacementActions.Skip, "page doesn't exist");
					}
					else if (toPages.Contains(move.Value))
					{
						this.actions[move.Key] = (action.Actions & ReplacementActions.Propose) != 0
							? this.HandleConflict(move.Key, move.Value)
							: new(ReplacementActions.Skip, "To page exists");
					}
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static void FilterTemplatesExceptDocs(TitleCollection titles)
		{
			for (var i = titles.Count - 1; i >= 0; i--)
			{
				if (titles[i] is Title title &&
					title.Namespace == MediaWikiNamespaces.Template &&
					!title.PageName.EndsWith("/doc", StringComparison.OrdinalIgnoreCase))
				{
					titles.RemoveAt(i);
				}
			}
		}

		private static void ProposeForDeletion(ContextualParser parser, string deletionText)
		{
			deletionText.ThrowNull();

			// Cheating and using text throughout, since this does not need to be parsed or acted upon currently, and is likely to be moved to another job soon anyway.
			var page = parser.NotNull().Page;
			var noinclude = page.Namespace == MediaWikiNamespaces.Template;
			if (!noinclude)
			{
				foreach (var backlink in page.Backlinks)
				{
					if (backlink.Value == BacklinksTypes.EmbeddedIn)
					{
						noinclude = true;
						break;
					}
				}
			}

			if (noinclude)
			{
				deletionText = "<noinclude>" + deletionText + "</noinclude>";
			}

			var insertPos = 0;
			string insertText;
			if (page.IsRedirect)
			{
				insertPos = parser.Count;
				insertText = '\n' + deletionText;
			}
			else
			{
				insertText = deletionText + '\n';
			}

			parser.Insert(insertPos, parser.Factory.TextNode(insertText));
		}
		#endregion

		#region Private Methods
		private void FullEdit(object sender, Page page)
		{
			ContextualParser parser = new(page);
			if (this.linkUpdates.TryGetValue(page, out var linkUpdate) &&
				this.actions[page].HasAction(ReplacementActions.NeedsEdited))
			{
				this.EditPageLoaded(parser, linkUpdate);
			}

			this.BacklinkPageLoaded(parser);
			parser.UpdatePage();
		}

		private void GetBacklinkTitles(PageCollection pageInfo, TitleCollection backlinkTitles)
		{
			foreach (var linkUpdate in this.linkUpdates)
			{
				if (pageInfo[linkUpdate.Key] is Page fromPage)
				{
					foreach (var backlink in fromPage.Backlinks)
					{
						backlinkTitles.Add(backlink.Key);
					}
				}
			}
		}

		private PageCollection GetFromPages()
		{
			var modules = this.PageInfoExtraModules | PageModules.Info;
			if ((this.FollowUpActions & FollowUpActions.AffectsBacklinks) != 0)
			{
				modules |= PageModules.Backlinks;
			}

			// Do not filter the From lists to only-existent pages, or backlinks and category members for non-existent pages will be missed.
			TitleCollection fromTitles = new(this.Site);
			foreach (var replacement in this.linkUpdates)
			{
				fromTitles.Add(replacement.Key);
			}

			var retval = PageCollection.Unlimited(this.Site, modules, false);
			retval.GetTitles(fromTitles);

			return retval;
		}

		private TitleCollection GetLoadTitles(PageCollection fromPages, IReadOnlyDictionary<Title, TitleCollection> categoryMembers)
		{
			TitleCollection backlinkTitles = new(this.Site);
			if ((this.FollowUpActions & FollowUpActions.AffectsBacklinks) != 0)
			{
				this.GetBacklinkTitles(fromPages, backlinkTitles);
				if (categoryMembers.Count > 0 && (this.FollowUpActions & FollowUpActions.UpdateCategoryMembers) != 0)
				{
					foreach (var catTitles in categoryMembers)
					{
						backlinkTitles.AddRange(catTitles.Value);
					}
				}

#if DEBUG
				backlinkTitles.Sort();
#endif
				this.FilterBacklinkTitles(backlinkTitles);
			}

			return backlinkTitles;
		}

		private PageCollection GetToPages()
		{
			var toPages = PageCollection.Unlimited(this.Site, PageModules.Info, false);
			if (this.MoveAction == MoveAction.MoveSafely)
			{
				TitleCollection toTitles = new(this.Site);
				foreach (var move in this.moves)
				{
					if (this.actions[move.Key].HasAction(ReplacementActions.Move))
					{
						toTitles.Add(move.Value);
					}
				}

				toPages.GetTitles(toTitles);
				toPages.RemoveExists(false);
			}

			return toPages;
		}

		private string UpdateGalleryLink(Page page, string line)
		{
			var link = SiteLink.FromGalleryText(this.Site, line);
			if (this.linkUpdates.TryGetValue(link, out var toTitle))
			{
				if (toTitle.Namespace != MediaWikiNamespaces.File)
				{
					this.Warn($"{link.PageName} to non-File {toTitle.FullPageName} move skipped in gallery on page: {page.FullPageName}.");
				}
				else
				{
					var newNamespace = (link.Namespace == toTitle.Namespace && link.Coerced)
						? this.Site[MediaWikiNamespaces.Main]
						: toTitle.Namespace;
					var newTitle = TitleFactory.FromValidated(newNamespace, toTitle.PageName);
					var newLink = link.WithTitle(newTitle);
					if (this.GetLinkText(page, link, newLink, false) is string newText)
					{
						newLink.Text = newText;
					}

					return newLink.ToString()[2..^2].TrimEnd();
				}
			}

			return line;
		}

		private void ValidateActions()
		{
			foreach (var action in this.actions)
			{
				if (action.Value.Actions == ReplacementActions.None)
				{
					this.Warn($"Action for {action.Key} is unknown.");
				}
			}
		}

		private void ValidateMoves()
		{
			Dictionary<Title, Title> unique = new();
			foreach (var move in this.moves)
			{
				var current = this.actions[move.Key];
				if (current.HasAction(ReplacementActions.Move))
				{
					if (unique.TryGetValue(move.Value, out var existing))
					{
						this.Warn($"Duplicate To title. All related entries will be skipped. [[{existing}]] and [[{move.Key}]] both moving to [[{move.Value}]]");
						this.actions[move.Value].SetMoveActions(ReplacementActions.Skip, "duplicate To page");
						current.SetMoveActions(ReplacementActions.Skip, "duplicate To page");
					}
					else
					{
						unique.Add(move.Value, move.Key);
					}
				}
			}
		}
		#endregion
	}
}
