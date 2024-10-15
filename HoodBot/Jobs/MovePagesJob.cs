namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Globalization;
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
		RetainDirectLinkText = 1 << 3,
		UpdateSameNamedText = 1 << 4,
		ProposeUnused = 1 << 5,
		UpdateCategoryMembers = 1 << 6,
		AffectsBacklinks = FixLinks | ProposeUnused | UpdateCategoryMembers,
		NeedsCategoryMembers = ProposeUnused | UpdateCategoryMembers,
		Default = CheckLinksRemaining | EmitReport | FixLinks | RetainDirectLinkText | UpdateSameNamedText,
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

	/// <summary>Underlying job to move pages.</summary>
	/// <seealso cref="EditJob" />
	public abstract class MovePagesJob : EditJob
	{
		#region Fields
		private readonly SortedDictionary<Title, DetailedActions> actions = [];
		private readonly Dictionary<Title, Title> linkUpdates = [];
		private readonly Dictionary<Title, Title> moves = [];
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
				this.Pages.NamespaceLimitations.Remove(MediaWikiNamespaces.User);
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
					List<string> list = [];
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
						var value = (this.SuppressRedirects ? "suppress" : "create") + " redirects";
						list.Add(value);
					}

					if (this.FollowUpActions.HasAnyFlag(FollowUpActions.FixLinks))
					{
						list.Add("fix links");
					}

					if (this.FollowUpActions.HasAnyFlag(FollowUpActions.UpdateCategoryMembers))
					{
						list.Add("re-categorize category members");
					}

					if (this.FollowUpActions.HasAnyFlag(FollowUpActions.ProposeUnused))
					{
						list.Add("propose unused pages for deletion");
					}

					if (this.FollowUpActions.HasAnyFlag(FollowUpActions.CheckLinksRemaining))
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

		protected IReadOnlyDictionary<Title, Title> LinkUpdates => this.linkUpdates;

		protected MoveAction MoveAction { get; set; } = MoveAction.MoveSafely;

		protected MoveOptions MoveExtra { get; set; }

		protected PageModules PageInfoExtraModules { get; set; }

		protected bool RecursiveCategoryMembers { get; set; } = true;

		protected bool SuppressRedirects { get; set; } = true;
		#endregion

		#region Protected Methods

		protected void AddLinkUpdate(string from, string to) => this.AddReplacement(from, to, ReplacementActions.None, null);

		protected void AddLinkUpdate(Title from, string to) => this.AddReplacement(from, to, ReplacementActions.None, null);

		protected void AddLinkUpdate(Title from, Title to) => this.AddReplacement(from, to, ReplacementActions.None, null);

		protected void AddMove(string from, string to) => this.AddReplacement(from, to, ReplacementActions.Move, null);

		protected void AddMove(Title from, string to) => this.AddReplacement(from, to, ReplacementActions.Move, null);

		protected void AddMove(Title from, Title to) => this.AddReplacement(from, to, ReplacementActions.Move, null);

		protected void AddReplacement(string from, string to, ReplacementActions initialActions, string? reason) => this.AddReplacement(
			TitleFactory.FromUnvalidated(this.Site, from),
			TitleFactory.FromUnvalidated(this.Site, to),
			initialActions,
			reason);

		protected void AddReplacement(Title from, string to, ReplacementActions initialActions, string? reason) => this.AddReplacement(
			from,
			TitleFactory.FromUnvalidated(this.Site, to),
			initialActions,
			reason);

		protected void AddReplacement(Title from, Title to, ReplacementActions initialActions, string? reason)
		{
			if (!this.AllowFromEqualsTo && from == to)
			{
				throw new InvalidOperationException($"From and To titles cannot be the same: {from}");
			}

			if (this.MoveAction != MoveAction.None)
			{
				this.moves.Add(from, to);
			}

			if (this.MoveAction == MoveAction.None ||
				initialActions == ReplacementActions.None ||
				this.FollowUpActions.HasAnyFlag(FollowUpActions.FixLinks))
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

		protected override string GetEditSummary(Page page) => this.EditSummaryUpdateLinks;

		protected override void LoadPages()
		{
			var fromPages = this.GetFromPages();
			this.ValidateMoveActions(fromPages);
			var categoryMembers = this.GetCategoryMembers(fromPages);
			var loadTitles = this.GetLoadTitles(fromPages, categoryMembers);

			this.StatusWriteLine("Figuring out what to do");
			if (this.FollowUpActions.HasAnyFlag(FollowUpActions.ProposeUnused))
			{
				this.SetupProposedDeletions(fromPages, categoryMembers);
			}

			if (this.MoveAction != MoveAction.None)
			{
				this.ValidateActions();
			}

			if (this.FollowUpActions.HasAnyFlag(FollowUpActions.EmitReport))
			{
				this.EmitReport();
				this.Results?.Save(); // Save prematurely so results are not lost in the event of a later problem.
			}

			this.StatusWriteLine("Getting linked pages");
			this.Pages.GetTitles(loadTitles);
		}

		protected override void Main()
		{
			if (this.MoveAction != MoveAction.None)
			{
				this.MovePages();
			}

			base.Main();
			if (this.FollowUpActions.HasAnyFlag(FollowUpActions.CheckLinksRemaining))
			{
				this.CheckRemaining();
			}
		}

		protected override void PageLoaded(Page page)
		{
			SiteParser parser = new(page);
			this.BacklinkPageLoaded(parser);
			parser.UpdatePage();
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void PopulateMoves();
		#endregion

		#region Protected Virtual Methods
		protected virtual void BacklinkPageLoaded(SiteParser parser)
		{
			ArgumentNullException.ThrowIfNull(parser);
			this.ReplaceBacklinks(parser.Page, parser);
		}

		protected virtual void CheckRemaining()
		{
			// Since links to either moved pages or the source of updated links are probably undesirable, we add both to the list to check.
			this.StatusWriteLine("Waiting for job queue to clear");
			this.Site.WaitForJobQueue();
			this.StatusWriteLine("Checking remaining pages");
			TitleCollection backlinkTitles = new(this.Site);
			foreach (var replacement in this.linkUpdates)
			{
				backlinkTitles.Add(replacement.Key);
			}

			foreach (var replacement in this.moves)
			{
				backlinkTitles.Add(replacement.Key);
			}

			TitleCollection leftovers = new(this.Site);
			var allBacklinks = PageCollection.Unlimited(this.Site, PageModules.Info | PageModules.Backlinks, false);
			allBacklinks.GetTitles(backlinkTitles);
			this.ResetProgress(allBacklinks.Count);
			foreach (var page in allBacklinks)
			{
				if (IsPageOrphaned(page))
				{
					this.OnFromPageOrphaned(page);
				}

				foreach (var leftover in page.Backlinks)
				{
					if (page.Title != leftover.Key)
					{
						// This time around, we add each backlink to the list, each of which will be purged.
						leftovers.TryAdd(leftover.Key);
					}
				}

				this.Progress++;
			}

			if (leftovers.Count > 0)
			{
				PageCollection.Purge(this.Site, leftovers, PurgeMethod.LinkUpdate, 5);
				Thread.Sleep(15); // Arbitrary wait to roughly ensure that job queue has started.
				this.StatusWriteLine("Waiting for job queue to clear");
				this.Site.WaitForJobQueue();

				this.StatusWriteLine("Double-checking leftover remaining pages");
				leftovers.Clear();
				allBacklinks.Clear();
				allBacklinks.GetTitles(backlinkTitles);
				this.ResetProgress(allBacklinks.Count);
				foreach (var page in allBacklinks)
				{
					var isOrphaned = IsPageOrphaned(page);
					if (isOrphaned)
					{
						this.OnFromPageOrphaned(page);
					}
					else
					{
						// We no longer care about which pages link back to the moved page; this time, we want the pages that still have backlinks.
						leftovers.Add(page.Title);
					}

					this.Progress++;
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

		protected virtual void CustomEdit(SiteParser parser, Title from)
		{
		}

		protected virtual void EmitReport()
		{
			this.WriteLine("{| class=\"wikitable sortable\"");
			this.WriteLine("! Title !! Action");
			foreach (var (from, action) in this.actions)
			{
				this.WriteLine("|-");
				this.Write(string.Create(CultureInfo.InvariantCulture, $"| {from} ([[Special:WhatLinksHere/{from}|links]]) || "));
				List<string> actionsList = [];
				if (action.HasAction(ReplacementActions.Skip))
				{
					actionsList.Add("skip");
				}
				else if (this.MoveAction != MoveAction.None && action.HasAction(ReplacementActions.Move))
				{
					actionsList.Add("move to " + SiteLink.ToText(this.moves[from], LinkFormat.ForcedLink));
					if (this.FollowUpActions.HasAnyFlag(FollowUpActions.FixLinks))
					{
						actionsList.Add("update links");
					}
				}
				else if (this.FollowUpActions.HasAnyFlag(FollowUpActions.FixLinks))
				{
					actionsList.Add("update links to " + SiteLink.ToText(this.linkUpdates[from], LinkFormat.ForcedLink));
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
			Dictionary<Title, TitleCollection> retval = [];
			if (this.FollowUpActions.HasAnyFlag(FollowUpActions.NeedsCategoryMembers))
			{
				var categoryReplacements = new Dictionary<Title, ITitle>();
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
					if (this.FollowUpActions.HasAnyFlag(FollowUpActions.ProposeUnused) ||
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

		protected virtual DetailedActions HandleConflict(Title from, ITitle to)
		{
			var action = this.actions[from];
			return new(action.Actions & ~ReplacementActions.Move, $"{SiteLink.ToText(to)} exists");
		}

		protected virtual Title? LinkUpdateMatch(SiteLink from)
		{
			// This whole function could be reduced to a one-liner but is separated out for easier reading/debugging.
			_ = this.linkUpdates.TryGetValue(from.Title, out var to);
			if (to is not null)
			{
				// If this is a category tag, make the change conditional on UpdateCategoryMembers flag.
				var isCategoryUpdate = from.Title.Namespace == MediaWikiNamespaces.Category && to.Namespace == MediaWikiNamespaces.Category;
				var doRename =
					!isCategoryUpdate ||
					this.FollowUpActions.HasAnyFlag(FollowUpActions.UpdateCategoryMembers) ||
					from.ForcedNamespaceLink;
				if (doRename)
				{
					return to;
				}
			}

			return null;
		}

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

				if (actionValue.HasAction(ReplacementActions.Edit))
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
					this.MoveExtra.HasAnyFlag(MoveOptions.MoveTalkPage);
				var moveSubPages =
					fromNs.AllowsSubpages &&
					this.MoveExtra.HasAnyFlag(MoveOptions.MoveSubPages);
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
					var parser = new SiteParser(editPage);
					var isMinor = true;
					string? editSummary = null;
					if (actionValue.HasAction(ReplacementActions.Edit))
					{
						editSummary = this.EditSummaryEditMovedPage;
						isMinor = this.GetIsMinorEdit(editPage);
						this.CustomEdit(parser, action.Key);
					}

					if (actionValue.HasAction(ReplacementActions.Propose))
					{
						var reason = action.Value.Reason;
						Globals.ThrowIfNull(reason, nameof(action), nameof(action.Value), nameof(action.Value.Reason));
						ProposeForDeletion(parser, "{{Proposeddeletion|bot=1|" + reason + "}}");
						editSummary = this.EditSummaryPropose;
						isMinor = false;
					}

					if (editSummary is null)
					{
						throw new InvalidOperationException("Edit summary is null editing moved page.");
					}

					parser.UpdatePage();
					editPage.SaveAs(to.FullPageName(), editSummary, isMinor, Tristate.False, false, true);
				}

				this.Progress++;
			}
		}

		protected virtual void OnFromPageOrphaned(Page page)
		{
		}

		protected virtual void ReplaceBacklinks(Page page, WikiNodeCollection nodes)
		{
			ArgumentNullException.ThrowIfNull(page);
			ArgumentNullException.ThrowIfNull(nodes);

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
				doNotDelete.GetBacklinks(template.FullPageName(), BacklinksTypes.EmbeddedIn, true);
			}

			foreach (var title in this.linkUpdates.Keys)
			{
				if (fromPages.GetMapped(title) is not Page fromPage ||
					!fromPage.Exists ||
					fromPage.Backlinks.Count != 0 ||
					catMembers.ContainsKey(fromPage.Title))
				{
					continue;
				}

				var action = this.actions[title];
				if (doNotDelete.Contains(fromPage.Title))
				{
					if (this.MoveAction != MoveAction.None)
					{
						action.SetMoveActions(ReplacementActions.Skip, "No links, but marked to not be deleted");
					}
				}
				else if (deletions.Contains(fromPage.Title))
				{
					action.SetMoveActions(ReplacementActions.Skip, "No links, but already proposed for deletion");
				}
				else
				{
					action.SetMoveActions(ReplacementActions.Propose, "Unused so propose for deletion");
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
						this.StatusWriteLine($"Malformed gallery link on {page.Title.FullPageName()} in line {line}");
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

		protected virtual SiteLink GetToLink(Page page, bool isRedirectTarget, SiteLink from, Title to)
		{
			var retval = from.WithTitle(to);
			this.UpdateLinkText(page, from, retval, !isRedirectTarget);
			return retval;
		}

		protected virtual void UpdateLinkNode(Page page, ILinkNode node, bool isRedirectTarget)
		{
			ArgumentNullException.ThrowIfNull(page);
			ArgumentNullException.ThrowIfNull(node);
			var from = SiteLink.FromLinkNode(this.Site, node);
			if (this.LinkUpdateMatch(from) is Title to)
			{
				this
					.GetToLink(page, isRedirectTarget, from, to)
					.UpdateLinkNode(node);
			}

			if (from.Title.Namespace == MediaWikiNamespaces.Media)
			{
				Title key = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.File], from.Title.PageName);
				if (this.linkUpdates.TryGetValue(key, out var toMedia))
				{
					this
						.GetToLink(page, isRedirectTarget, from, TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Media], toMedia!.PageName))
						.UpdateLinkNode(node);
				}
			}
		}

		protected virtual void UpdateLinkText(ITitle page, SiteLink from, SiteLink toLink, bool addCaption)
		{
			ArgumentNullException.ThrowIfNull(page);
			ArgumentNullException.ThrowIfNull(from);
			ArgumentNullException.ThrowIfNull(toLink);
			if (addCaption &&
				toLink.Text is null &&
				from.OriginalTitle is not null &&
				this.FollowUpActions.HasAnyFlag(FollowUpActions.RetainDirectLinkText))
			{
				// If there's no link text then we want to preserve the previous display text for any caption-less links by adding a new caption. Logic further up sets addCaption appropriately so this won't occur for a redirect target or in galleries.
				toLink.Text = from.OriginalTitle.TrimStart(TextArrays.Colon);
			}

			if (this.FollowUpActions.HasAnyFlag(FollowUpActions.UpdateSameNamedText) && from.Text is not null)
			{
				// Parse the original text and see if it matches either FullPageName or PageName. If so, update it, retaining interwiki text and anchors, if present.
				var textLink = TitleFactory.FromUnvalidated(this.Site, from.Text).ToSiteLink();
				if (textLink.Title.PageNameEquals(from.Title.PageName))
				{
					if (textLink.Title.Namespace == from.Title.Namespace)
					{
						var retitled = textLink.WithTitle(toLink.Title);
						toLink.Text = retitled.AsLink()[2..^2];
					}
					else if (textLink.Title.Namespace == MediaWikiNamespaces.Main)
					{
						var title = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Main], toLink.Title.PageName);
						var retitled = textLink.WithTitle(title);
						toLink.Text = retitled.AsLink()[2..^2];
					}
				}
			}
		}

		protected virtual void UpdateTemplateNode(Page page, SiteTemplateNode template)
		{
			ArgumentNullException.ThrowIfNull(page);
			ArgumentNullException.ThrowIfNull(template);
			var fullTitle = new FullTitle(template.Title);
			if (this.linkUpdates.TryGetValue(fullTitle.Title, out var to))
			{
				var nameText = to.Namespace == MediaWikiNamespaces.Template
					? to.PageName
					: to.Namespace.DecoratedName() + to.PageName;
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
					if (fromPages.GetMapped(move.Key) is Page fromPage && !fromPage.Exists)
					{
						// From title does not exist. Should still have an entry in linkUpdates, if appropriate.
						this.actions[move.Key] = new(ReplacementActions.Skip, $"{SiteLink.ToText(fromPage)} doesn't exist");
					}
					else if (toPages.Contains(move.Value))
					{
						this.actions[move.Key] = action.Actions.HasAnyFlag(ReplacementActions.Propose)
							? this.HandleConflict(move.Key, move.Value)
							: new(ReplacementActions.Skip, $"{SiteLink.ToText(move.Value)} exists");
					}
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static void GetBacklinkTitles(PageCollection pageInfo, TitleCollection backlinkTitles)
		{
			foreach (var page in pageInfo)
			{
				backlinkTitles.AddRange(page.Backlinks.Keys);
			}
		}

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

		private static bool IsPageOrphaned(Page page)
		{
			var unique = new SortedSet<Title>(page.Backlinks.Keys);
			return unique.Count == 0 || (unique.Count == 1 && unique.Contains(page.Title));
		}

		private static void ProposeForDeletion(SiteParser parser, string deletionText)
		{
			ArgumentNullException.ThrowIfNull(parser);
			ArgumentNullException.ThrowIfNull(deletionText);

			// Cheating and using text throughout, since this does not need to be parsed or acted upon currently, and is likely to be moved to another job soon anyway.
			var page = parser.Page;
			var noinclude = page.Title.Namespace == MediaWikiNamespaces.Template;
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
		private PageCollection GetFromPages()
		{
			var modules = this.PageInfoExtraModules | PageModules.Info;
			if (this.FollowUpActions.HasAnyFlag(FollowUpActions.AffectsBacklinks))
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
			if (this.FollowUpActions.HasAnyFlag(FollowUpActions.AffectsBacklinks))
			{
				GetBacklinkTitles(fromPages, backlinkTitles);
				if (categoryMembers.Count > 0 && this.FollowUpActions.HasAnyFlag(FollowUpActions.UpdateCategoryMembers))
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
			if (this.linkUpdates.TryGetValue(link.Title, out var toTitle))
			{
				if (toTitle.Namespace != MediaWikiNamespaces.File)
				{
					this.Warn($"{link.Title.PageName} to non-File {toTitle.FullPageName()} move skipped in gallery on page: {page.Title.FullPageName()}.");
				}
				else
				{
					var newNamespace = (link.Title.Namespace == MediaWikiNamespaces.File && link.Coerced)
						? this.Site[MediaWikiNamespaces.Main]
						: toTitle.Namespace;
					var newTitle = TitleFactory.FromValidated(newNamespace, toTitle.PageName);
					var newLink = link.WithTitle(newTitle);
					this.UpdateLinkText(page, link, newLink, false);

					return newLink.LinkTarget(false);
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
			Dictionary<Title, Title> unique = [];
			foreach (var move in this.moves)
			{
				var title = move.Key;
				var current = this.actions[title];
				if (current.HasAction(ReplacementActions.Move))
				{
					if (unique.TryGetValue(title, out var existing))
					{
						this.Warn($"Duplicate To title. All related entries will be skipped. [[{existing}]] and [[{move.Key}]] both moving to [[{move.Value}]]");
						this.actions[title].SetMoveActions(ReplacementActions.Skip, "Duplicate To page");
						current.SetMoveActions(ReplacementActions.Skip, "Duplicate To page");
					}
					else
					{
						unique.Add(title, move.Value);
					}
				}
			}
		}
		#endregion
	}
}