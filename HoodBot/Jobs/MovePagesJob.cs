namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using System.Text;
	using System.Threading;
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
		private readonly IDictionary<Title, DetailedActions> actions = new SortedDictionary<Title, DetailedActions>(SimpleTitleComparer.Instance);
		private readonly IDictionary<Title, Title> linkUpdates = new Dictionary<Title, Title>(SimpleTitleComparer.Instance);
		private readonly IDictionary<Title, Title> moves = new Dictionary<Title, Title>(SimpleTitleComparer.Instance);
		private readonly ParameterReplacers parameterReplacers;

		private bool isRedirectLink;
		private string? logDetails;
		#endregion

		#region Constructors
		protected MovePagesJob(JobManager jobManager, bool updateUserSpace)
			: base(jobManager)
		{
			this.parameterReplacers = new ParameterReplacers(jobManager.Site, this.LinkUpdates);
			if (updateUserSpace)
			{
				this.Pages.SetLimitations(
					LimitationType.Disallow,
					MediaWikiNamespaces.Media,
					MediaWikiNamespaces.MediaWiki,
					MediaWikiNamespaces.Special,
					MediaWikiNamespaces.Template);
			}
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

		#region Protected Override Properties
		protected override string EditSummary => this.EditSummaryUpdateLinks;
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

		// [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Optiona, to be called only when necessary.")]
		protected void LoadReplacementsFromFile(string fileName, ReplacementActions actions)
		{
			var repFile = File.ReadLines(fileName);
			foreach (var line in repFile)
			{
				var rep = line.Split(TextArrays.Tab);
				var from = TitleFactory.FromUnvalidated(this.Site, rep[0].Trim());
				var to = TitleFactory.FromUnvalidated(this.Site, rep[1].Trim());
				this.AddReplacement(from, to, actions, null);
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			this.StatusWriteLine("Getting Replacement List");
			this.PopulateMoves();
			if (this.MoveAction != MoveAction.None)
			{
				this.ValidateMoves();
			}
		}

		protected override void LoadPages()
		{
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
				this.Results?.Save(); // Save prematurely so results are not lost in the event of a later problem.
			}

			this.Pages.GetTitles(loadTitles);
		}

		protected override void Main()
		{
			if (this.MoveAction != MoveAction.None)
			{
				this.MovePages();
			}

			base.Main();
			if ((this.FollowUpActions & FollowUpActions.CheckLinksRemaining) != 0)
			{
				this.CheckRemaining();
			}
		}

		protected override void PageLoaded(Page page)
		{
			ContextualParser parser = new(page);
			this.BacklinkPageLoaded(parser);
			parser.UpdatePage();
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void PopulateMoves();
		#endregion

		#region Protected Virtual Methods
		protected virtual void BacklinkPageLoaded(ContextualParser parser) => this.ReplaceBacklinks(parser.NotNull().Page, parser);

		protected virtual void CheckRemaining()
		{
			this.Site.WaitForJobQueue();
			this.StatusWriteLine("Checking remaining pages");
			TitleCollection backlinkTitles = new(this.Site);
			foreach (var replacement in this.linkUpdates)
			{
				backlinkTitles.Add(replacement.Key);
			}

			TitleCollection leftovers = new(this.Site);
			var allBacklinks = PageCollection.Unlimited(this.Site, PageModules.Info | PageModules.Backlinks, false);
			allBacklinks.GetTitles(backlinkTitles);
			foreach (var page in allBacklinks)
			{
				foreach (var leftover in page.Backlinks)
				{
					if (!page.SimpleEquals(leftover.Key))
					{
						// This time around, we add each backlink to the list, each of which will be purged.
						leftovers.Add(leftover.Key);
					}
				}
			}

			if (leftovers.Count > 0)
			{
				PageCollection.Purge(this.Site, leftovers, PurgeMethod.LinkUpdate, 5);
				Thread.Sleep(15); // Arbitrary wait to roughly ensure that job queue has started.
				this.Site.WaitForJobQueue();

				leftovers.Clear();
				allBacklinks.Clear();
				allBacklinks.GetTitles(backlinkTitles);
				foreach (var page in allBacklinks)
				{
					if ((page.Backlinks.Count > 1 && !page.Backlinks.ContainsKey(page)) || page.Backlinks.Count > 1)
					{
						// We no longer care about which pages link back to the moved page; this time, we want the pages that still have backlinks.
						leftovers.Add(page);
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
		}

		protected virtual void CustomEdit(ContextualParser parser, Title from)
		{
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
				if (action.HasAction(ReplacementActions.Skip))
				{
					actionsList.Add("skip");
				}
				else if (this.MoveAction != MoveAction.None && action.HasAction(ReplacementActions.Move))
				{
					actionsList.Add("move to " + this.moves[from].AsLink());
					if (this.FollowUpActions.HasFlag(FollowUpActions.FixLinks))
					{
						actionsList.Add("update links");
					}
				}
				else if (this.FollowUpActions.HasFlag(FollowUpActions.FixLinks))
				{
					actionsList.Add("update links to " + this.linkUpdates[from].AsLink());
				}

				if (action.HasAction(ReplacementActions.Edit) && !action.HasAction(ReplacementActions.Skip))
				{
					actionsList.Add("edit" + (action.HasAction(ReplacementActions.Move) ? " moved page" : string.Empty));
				}

				if (action.HasAction(ReplacementActions.Propose))
				{
					actionsList.Add("propose for deletion");
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
			var moveCount = 0;
			var editTitles = new TitleCollection(this.Site);
			foreach (var action in this.actions)
			{
				var actionValue = action.Value;
				if (actionValue.HasAction(ReplacementActions.Move))
				{
					moveCount++;
				}

				if (actionValue.HasAction(ReplacementActions.Edit) || actionValue.HasAction(ReplacementActions.Edit))
				{
					editTitles.Add(action.Key);
				}
			}

			var editPages = editTitles.Load();
			this.ProgressMaximum = moveCount;
			foreach (var action in this.actions)
			{
				var to = this.moves[action.Key];
				var fromNs = action.Key.Namespace;
				var moveTalkPage =
					!fromNs.IsTalkSpace &&
					(this.MoveExtra & MoveOptions.MoveTalkPage) != 0;
				var moveSubPages =
					fromNs.AllowsSubpages &&
					(this.MoveExtra & MoveOptions.MoveSubPages) != 0;
				this.Site.Move(
					action.Key,
					to,
					this.EditSummaryMove,
					moveTalkPage,
					moveSubPages,
					this.SuppressRedirects);
				if (editPages.TryGetValue(action.Key, out var editPage))
				{
					var actionValue = action.Value;
					var parser = new ContextualParser(editPage);
					var isMinor = true;
					string? editSummary = null;
					if (actionValue.HasAction(ReplacementActions.Edit))
					{
						editSummary = this.EditSummaryEditMovedPage;
						isMinor = this.MinorEdit;
						this.CustomEdit(parser, action.Key);
					}

					if (actionValue.HasAction(ReplacementActions.Propose))
					{
						var reason = action.Value.Reason.NotNull();
						ProposeForDeletion(parser, "{{Proposeddeletion|bot=1|" + reason.UpperFirst(this.Site.Culture) + "}}");
						editSummary = this.EditSummaryPropose;
						isMinor = false;
					}

					if (editSummary is null)
					{
						throw new InvalidOperationException("Edit summary is null editing moved page.");
					}

					parser.UpdatePage();
					editPage.SaveAs(to.FullPageName, editSummary, isMinor, Tristate.False, false, true);
				}

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
						this.actions[move.Key] = new(ReplacementActions.Skip, $"{fromPage.AsLink()} doesn't exist");
					}
					else if (toPages.Contains(move.Value))
					{
						this.actions[move.Key] = (action.Actions & ReplacementActions.Propose) != 0
							? this.HandleConflict(move.Key, move.Value)
							: new(ReplacementActions.Skip, $"{move.Value.AsLink()} exists");
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
		private void GetBacklinkTitles(PageCollection pageInfo, TitleCollection backlinkTitles)
		{
			foreach (var linkUpdate in this.linkUpdates)
			{
				if (pageInfo.TryGetValue(linkUpdate.Key, out var fromPage))
				{
					foreach (var backlink in fromPage.Backlinks)
					{
						backlinkTitles.Add(backlink.Key);
					}
				}
				else
				{
					this.StatusWriteLine("Key not found: " + linkUpdate.Key);
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

		private TitleCollection LoadProposedDeletions()
		{
			TitleCollection deleted = new(this.Site);
			foreach (var title in this.Site.DeletionCategories)
			{
				deleted.GetCategoryMembers(title.PageName);
			}

			return deleted;
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
