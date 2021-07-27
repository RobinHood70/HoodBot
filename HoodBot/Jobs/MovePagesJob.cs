namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.IO;
	using System.Text;
	using System.Threading;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
	using static RobinHood70.CommonCode.Globals;

	#region Internal Enums
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

	public enum RedirectOption
	{
		Suppress,
		Create,
		CreateButProposeDeletion
	}
	#endregion

	// TODO: Split out ProposeUnused code into a separate job; have this one only check, not change.

	/// <summary>Underlying job to move pages. This partial class contains the job logic, while the other one contains the parameter replacer methods.</summary>
	/// <seealso cref="EditJob" />
	public abstract class MovePagesJob : EditJob
	{
		#region Fields
		private readonly string replacementStatusFile;
		private readonly ParameterReplacers parameterReplacers;
		private readonly SimpleTitleJsonConverter titleConverter;

		private bool isFirstLink;
		private string? logDetails;
		#endregion

		#region Constructors
		protected MovePagesJob(JobManager jobManager)
			: this(jobManager, null)
		{
		}

		protected MovePagesJob(JobManager jobManager, string? replacementName)
			: base(jobManager)
		{
			this.parameterReplacers = new ParameterReplacers(this);
			this.titleConverter = new(this.Site);
			replacementName = replacementName == null ? string.Empty : " - " + replacementName;
			this.replacementStatusFile = UespSite.GetBotDataFolder($"Replacements{replacementName}.json");
		}
		#endregion

		#region Public Properties
		public KeyedCollection<Title, Replacement> Replacements { get; } = new ReplacementCollection();
		#endregion

		#region Public Override Properties

		public override string LogDetails
		{
			get
			{
				if (this.logDetails == null)
				{
					var list = new List<string>();
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
						var value = this.RedirectOption switch
						{
							RedirectOption.Suppress => "suppress redirects",
							RedirectOption.Create => "create redirects",
							_ => "create redirects but propose them for deletion",
						};
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
		protected bool DeleteOnSuccess { get; set; } = true;

		protected string EditSummaryEditMovedPage { get; set; } = "Update moved page text";

		protected string EditSummaryMove { get; set; } = "Rename";

		protected string EditSummaryPropose { get; set; } = "Propose for deletion";

		protected string EditSummaryUpdateLinks { get; set; } = "Update links after page move";

		protected FollowUpActions FollowUpActions { get; set; } = FollowUpActions.Default;

		protected MoveAction MoveAction { get; set; } = MoveAction.MoveSafely;

		protected int MoveDelay { get; set; }

		protected MoveOptions MoveExtra { get; set; }

		protected PageModules PageInfoExtraModules { get; set; }

		protected bool RecursiveCategoryMembers { get; set; } = true;

		protected RedirectOption RedirectOption { get; set; }

		protected bool SuppressRedirects { get; set; } = true;
		#endregion

		#region Protected Methods
		protected void AddReplacement(string from, string to) => this.AddReplacement(
			Title.FromName(this.Site, from),
			Title.FromName(this.Site, to));

		protected void AddReplacement(Title from, Title to) => this.Replacements.Add(new Replacement(from, to));

		protected void AddReplacement(string from, string to, ReplacementActions initialActions) => this.AddReplacement(
			Title.FromName(this.Site, from),
			Title.FromName(this.Site, to),
			initialActions);

		protected void AddReplacement(Title from, Title to, ReplacementActions initialActions) => this.Replacements.Add(new Replacement(from, to) { Actions = initialActions });

		protected void DeleteStatusFile() => File.Delete(this.replacementStatusFile);

		// [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Optiona, to be called only when necessary.")]
		protected void LoadReplacementsFromFile(string fileName)
		{
			var repFile = File.ReadLines(fileName);
			foreach (var line in repFile)
			{
				var rep = line.Split(TextArrays.Tab);
				this.AddReplacement(rep[0].Trim(), rep[1].Trim());
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.StatusWriteLine("Getting Replacement List");
			var readFromFile = File.Exists(this.replacementStatusFile);
			if (readFromFile)
			{
				this.ReadJsonFile();
			}
			else
			{
				this.PopulateReplacements();
				if (this.MoveAction != MoveAction.None)
				{
					this.ValidateMoves();
				}
			}

			((ReplacementCollection)this.Replacements).Sort();
			var pageInfo = this.LoadPageInfo();
			var categoryMembers = this.GetCategoryMembers();
			if (!readFromFile)
			{
				this.StatusWriteLine("Figuring out what to do");
				this.SetupMoves(pageInfo);
				this.SetupProposedDeletions(pageInfo, categoryMembers);
				this.ValidateActions();
				this.WriteJsonFile();
			}

			if ((this.FollowUpActions & FollowUpActions.EmitReport) != 0)
			{
				this.EmitReport();
			}

			var loadTitles = new TitleCollection(this.Site);
			if ((this.FollowUpActions & FollowUpActions.AffectsBacklinks) != 0)
			{
				// This can be long, so we do it beforehand and save it, rather than showing a lengthy pause between moving pages and fixing links. This will also potentially allow a merger with this.editDictionary later.
				var backlinkTitles = this.GetBacklinkTitles(pageInfo);
				if ((this.FollowUpActions & FollowUpActions.UpdateCategoryMembers) != 0)
				{
					foreach (var replacement in this.Replacements)
					{
						if ((replacement.Actions & ReplacementActions.UpdateLinks) != 0 &&
						replacement.From.Namespace == replacement.To.Namespace)
						{
							var catMembers = categoryMembers[replacement.From];
							backlinkTitles.AddRange(catMembers);
						}
					}
				}

#if DEBUG
				backlinkTitles.Sort();
#endif
				this.FilterBacklinkTitles(backlinkTitles);

				if ((this.FollowUpActions & FollowUpActions.FixLinks) != 0)
				{
					loadTitles.AddRange(backlinkTitles);
				}
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

			if (this.DeleteOnSuccess)
			{
				this.DeleteStatusFile();
			}
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void PopulateReplacements();
		#endregion

		#region Protected Virtual Methods
		protected virtual void BacklinkPageLoaded(ContextualParser parser)
		{
			// Page may not have been correctly found if it was recently moved. If it wasn't, there's little we can do here, so skip it and it'll show up in the report (assuming it's generated).
			// TODO: See if this can be worked around, like asking the wiki to purge and reload or something.
			ThrowNull(parser, nameof(parser));
			this.ReplaceBacklinks((Page)parser.Context, parser.Nodes); // TODO: See if this can be re-written with ContextualParser methods.
		}

		protected virtual void CheckRemaining()
		{
			this.StatusWriteLine("Checking remaining pages");
			var leftovers = new TitleCollection(this.Site);
			var allBacklinks = PageCollection.Unlimited(this.Site, PageModules.Info | PageModules.Backlinks, false);
			var backlinkTitles = new TitleCollection(this.Site);
			foreach (var item in this.Replacements)
			{
				backlinkTitles.Add(item.From);
			}

			allBacklinks.GetTitles(backlinkTitles);
			foreach (var page in allBacklinks)
			{
				foreach (var backlink in page.Backlinks)
				{
					if (!page.SimpleEquals(backlink.Key))
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

		protected virtual void CustomEdit(Page page, Replacement replacement)
		{
		}

		protected virtual void EditPageLoaded(ContextualParser parser, Replacement replacement)
		{
			var page = (Page)parser.Context;
			if (
				page.Exists &&
				(this.FollowUpActions & FollowUpActions.ProposeUnused) != 0 &&
				(replacement.Actions & ReplacementActions.Propose) != 0)
			{
				ThrowNull(replacement.Reason, nameof(replacement), nameof(replacement.Reason));
				ProposeForDeletion(parser, "{{Proposeddeletion|bot=1|" + replacement.Reason.UpperFirst(this.Site.Culture) + "}}");
				this.SaveInfo[replacement.From] = new SaveInfo(this.EditSummaryPropose, false);
			}

			if ((replacement.Actions & ReplacementActions.Edit) != 0)
			{
				this.CustomEdit(page, replacement);
			}
		}

		protected virtual void EmitReport()
		{
			this.WriteLine("{| class=\"wikitable sortable\"");
			this.WriteLine("! Page !! Action");
			foreach (var replacement in this.Replacements)
			{
				this.WriteLine("|-");
				this.Write(Invariant($"| {replacement.From} ([[Special:WhatLinksHere/{replacement.From}|links]]) || "));
				var actions = new List<string>();
				if (this.MoveAction != MoveAction.None)
				{
					if ((replacement.Actions & ReplacementActions.Move) != 0)
					{
						actions.Add("move to " + replacement.To.AsLink(false));
					}
				}
				else if ((replacement.Actions & ReplacementActions.UpdateLinks) != 0)
				{
					actions.Add("update links to " + replacement.To.AsLink(false));
				}

				if ((replacement.Actions & ReplacementActions.Edit) != 0)
				{
					actions.Add("edit" + ((replacement.Actions & ReplacementActions.Move) != 0 ? " moved page" : string.Empty));
				}

				if ((replacement.Actions & ReplacementActions.Propose) != 0)
				{
					actions.Add("propose for deletion");
				}

				if ((replacement.Actions & ReplacementActions.Skip) != 0)
				{
					actions.Add("skip");
				}

				if (actions.Count == 0)
				{
					actions.Add("none");
				}

				var action = string.Join(", ", actions).UpperFirst(this.Site.Culture);
				if (replacement.Reason != null)
				{
					action += " (" + replacement.Reason + ")";
				}

				this.WriteLine(action);
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

		protected virtual IReadOnlyDictionary<Title, TitleCollection> GetCategoryMembers()
		{
			var retval = new Dictionary<Title, TitleCollection>();
			if ((this.FollowUpActions & FollowUpActions.NeedsCategoryMembers) != 0)
			{
				this.StatusWriteLine("Getting category members");
				this.ResetProgress(this.Replacements.Count);
				foreach (var replacement in this.Replacements)
				{
					if (replacement.From.Namespace == MediaWikiNamespaces.Category)
					{
						var updateMembers =
							(this.FollowUpActions & FollowUpActions.UpdateCategoryMembers) != 0 &&
							(replacement.Actions & ReplacementActions.UpdateLinks) != 0 &&
							replacement.From.Namespace == replacement.To.Namespace;
						if (updateMembers || (this.FollowUpActions & FollowUpActions.ProposeUnused) != 0)
						{
							var catMembers = new TitleCollection(this.Site);
							catMembers.GetCategoryMembers(replacement.From.PageName, CategoryMemberTypes.All, this.RecursiveCategoryMembers);
							if (catMembers.Count > 0)
							{
								retval.Add(replacement.From, catMembers);
							}
						}
					}

					this.Progress++;
				}
			}

			return retval;
		}

		protected virtual void HandleConflict(Replacement replacement)
		{
			ThrowNull(replacement, nameof(replacement));
			replacement.Actions &= ~ReplacementActions.Move;
			replacement.Reason = $"{replacement.To.AsLink(false)} exists";
		}

		protected virtual void MovePages()
		{
			this.StatusWriteLine("Moving pages");
			this.Progress = 0;
			this.ProgressMaximum = this.Replacements.Count;
			foreach (var replacement in this.Replacements)
			{
				if ((replacement.Actions & ReplacementActions.Move) != 0)
				{
					var fromTitle = replacement.From;
					var moveTalkPage = (this.MoveExtra & MoveOptions.MoveTalkPage) != 0 && fromTitle.Namespace.IsTalkSpace;
					var moveSubPages = (this.MoveExtra & MoveOptions.MoveSubPages) != 0 && fromTitle.Namespace.AllowsSubpages;
					fromTitle.Move(
						replacement.To.FullPageName,
						this.EditSummaryMove,
						moveTalkPage,
						moveSubPages,
						this.RedirectOption == RedirectOption.Suppress);
					if (this.MoveDelay > 0)
					{
						// Quick hack of a delay, since UESP sometimes seems to lag, but may be version-specific, so don't want to make it too formal of a thing.
						Thread.Sleep(this.MoveDelay);
					}
				}

				this.Progress++;
			}
		}

		protected virtual void ReplaceBacklinks(Page page, NodeCollection nodes)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(nodes, nameof(nodes));

			// Possibly better as a visitor class?
			this.isFirstLink = true;
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
				case SiteLinkNode link:
					this.UpdateLinkNode(page, link, page.IsRedirect && this.isFirstLink);
					this.isFirstLink = false;
					break;
				case SiteTemplateNode template:
					this.UpdateTemplateNode(page, template);
					break;
			}
		}

		protected virtual void SetupMoves(PageCollection pageInfo)
		{
			var toPages = PageCollection.Unlimited(this.Site, PageModules.Info, false);
			if (this.MoveAction == MoveAction.MoveSafely)
			{
				var toTitles = new TitleCollection(this.Site);
				foreach (var replacement in this.Replacements)
				{
					if ((replacement.Actions & ReplacementActions.Move) != 0)
					{
						toTitles.Add(replacement.To);
					}
				}

				toPages.GetTitles(toTitles);
				toPages.RemoveExists(false);
			}

			foreach (var replacement in this.Replacements)
			{
				if ((replacement.Actions & ReplacementActions.Skip) == 0)
				{
					var fromPage = pageInfo[replacement.From];
					if (this.MoveAction == MoveAction.None && !replacement.From.SimpleEquals(replacement.To))
					{
						replacement.Actions |= ReplacementActions.UpdateLinks;
					}
					else
					{
						// Any proposed move.
						if (!fromPage.Exists)
						{
							// From title does not exist.
							replacement.Actions = ReplacementActions.Skip;
							replacement.Reason = "page doesn't exist";
						}
						else if ((replacement.Actions & ReplacementActions.Move) != 0 && toPages.Contains(replacement.To))
						{
							replacement.Actions = ReplacementActions.Skip;
							if ((replacement.Actions & ReplacementActions.Propose) != 0)
							{
								this.HandleConflict(replacement);
							}

							// HandleConflict may have resolved the issue and changed the flag, so check again.
							if ((replacement.Actions & ReplacementActions.Skip) != 0)
							{
								replacement.Reason ??= "To page exists";
							}
						}
						else if (!replacement.From.SimpleEquals(replacement.To))
						{
							replacement.Actions |= ReplacementActions.Move | ReplacementActions.UpdateLinks;
							if (this.RedirectOption == RedirectOption.CreateButProposeDeletion && !fromPage.IsRedirect)
							{
								replacement.Actions |= ReplacementActions.Propose;
								replacement.Reason = "redirect from page move";
							}
						}
					}
				}
			}
		}

		protected virtual void SetupProposedDeletions(PageCollection pageInfo, IReadOnlyDictionary<Title, TitleCollection> catMembers)
		{
			if ((this.FollowUpActions & FollowUpActions.ProposeUnused) != 0)
			{
				var deletions = this.LoadProposedDeletions();
				var doNotDelete = new TitleCollection(this.Site);
				foreach (var template in this.Site.DeletePreventionTemplates)
				{
					doNotDelete.GetBacklinks(template.FullPageName, BacklinksTypes.EmbeddedIn, true);
				}

				foreach (var replacement in this.Replacements)
				{
					var fromPage = pageInfo[replacement.From];
					if (fromPage.Exists &&
						fromPage.Backlinks.Count == 0 &&
						!catMembers.ContainsKey(fromPage))
					{
						if (doNotDelete.Contains(fromPage))
						{
							if (this.MoveAction != MoveAction.None)
							{
								replacement.Actions |= ReplacementActions.Move;
								replacement.Reason = "no links, but marked to not be deleted";
							}
						}
						else if (deletions.Contains(fromPage))
						{
							replacement.Actions = ReplacementActions.Skip;
							replacement.Reason = "already proposed for deletion";
						}
						else
						{
							replacement.Actions |= ReplacementActions.Propose;
							replacement.Reason = "appears to be unused";
						}
					}
				}
			}
		}

		protected virtual void UpdateGalleryLinks(Page page, ITagNode tag)
		{
			ThrowNull(tag, nameof(tag));
			var text = tag.InnerText;
			if (text == null)
			{
				return;
			}

			var sb = new StringBuilder();
			var lines = text.Split(TextArrays.LineFeed);
			foreach (var line in lines)
			{
				var newLine = line;
				if (line.Length > 0)
				{
					var link = SiteLink.FromGalleryText(this.Site, line);
					if (this.Replacements.TryGetValue(link, out var replacement)
						&& (replacement.Actions & ReplacementActions.UpdateLinks) != 0)
					{
						if (replacement.To.Namespace != MediaWikiNamespaces.File)
						{
							this.Warn($"{replacement.From.PageName} to non-File {replacement.From.FullPageName} move skipped in gallery on page: {page.FullPageName}.");
							continue;
						}

						var newPageName = replacement.To.PageName;
						var newNamespace = (replacement.From.Namespace == replacement.To.Namespace && link.Coerced) ? this.Site[MediaWikiNamespaces.Main] : replacement.To.Namespace;
						var newLink = link.With(newNamespace, newPageName);
						this.UpdateLinkText(page, replacement.From, newLink, false);
						newLine = newLink.ToString()[2..^2].TrimEnd();
					}
				}

				sb.Append(newLine).Append('\n');
			}

			if (sb.Length > 0)
			{
				sb.Remove(sb.Length - 1, 1);
			}

			tag.InnerText = sb.ToString();
		}

		protected virtual void UpdateLinkNode(Page page, SiteLinkNode node, bool isRedirectTarget)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(node, nameof(node));
			var link = SiteLink.FromLinkNode(this.Site, node);
			if (this.Replacements.TryGetValue(link, out var replacement)
				&& (replacement.Actions & ReplacementActions.UpdateLinks) != 0
				&& (link.ForcedNamespaceLink
					|| link.Namespace != MediaWikiNamespaces.Category
					|| replacement.To.Namespace != MediaWikiNamespaces.Category
					|| (this.FollowUpActions & FollowUpActions.UpdateCategoryMembers) != 0))
			{
				link = link.With(replacement.To);
				this.UpdateLinkText(page, replacement.From, link, !isRedirectTarget);
				link.UpdateLinkNode(node);
			}

			if (link.Namespace == MediaWikiNamespaces.Media
				&& this.Replacements.TryGetValue(link.With(this.Site[MediaWikiNamespaces.File], link.PageName), out replacement)
				&& (replacement.Actions & ReplacementActions.UpdateLinks) != 0)
			{
				link = link.With(this.Site[MediaWikiNamespaces.Media], replacement.To.PageName);
				this.UpdateLinkText(page, replacement.From, link, !isRedirectTarget);
				link.UpdateLinkNode(node);
			}
		}

		protected virtual void UpdateLinkText(Page page, Title oldTitle, SiteLink newLink, bool addCaption)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(oldTitle, nameof(oldTitle));
			ThrowNull(newLink, nameof(newLink));
			if ((this.FollowUpActions & FollowUpActions.UpdateCaption) != 0)
			{
				// If UpdateCaption is true and caption exactly matches either the full page name or the simple page name, update it.
				if (newLink.Text != null
					&& page.Namespace != MediaWikiNamespaces.User
					&& !page.Site.IsDiscussionPage(page))
				{
					var textTitle = new TitleParser(this.Site, newLink.Text);
					if (oldTitle is IFullTitle fullTitle && fullTitle.FullEquals(textTitle))
					{
						newLink.Text = textTitle.ToString();
					}
					else if (oldTitle.SimpleEquals(textTitle))
					{
						var simp = this.Replacements[oldTitle].To;
						newLink.Text = simp.ToString();
					}
					else if ((this.FollowUpActions & FollowUpActions.UpdatePageNameCaption) != 0 && string.Equals(oldTitle.PageName, newLink.Text, StringComparison.Ordinal))
					{
						var simp = this.Replacements[oldTitle].To;
						newLink.Text = simp.PageName;
					}
				}
			}
			else if (addCaption
				&& newLink.Text == null
				&& newLink.OriginalLink != null
				&& (newLink.ForcedNamespaceLink || !newLink.Namespace.IsForcedLinkSpace))
			{
				// if UpdateCaption is false, then we want to preserve the previous display text for any caption-less links by adding a new caption. Logic further up sets addCaption appropriately so this won't occur for a redirect target or in galleries.
				newLink.Text = newLink.OriginalLink.TrimStart(':');
			}
		}

		protected virtual void UpdateTemplateNode(Page page, SiteTemplateNode template)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(template, nameof(template));

			if (this.Replacements.TryGetValue(template.TitleValue, out var replacement) &&
				(replacement.Actions & ReplacementActions.UpdateLinks) != 0)
			{
				var newTemplate = replacement.To;
				var nameText = newTemplate.Namespace == MediaWikiNamespaces.Template ? newTemplate.PageName : newTemplate.FullPageName;
				template.SetTitle(nameText);
			}

			this.parameterReplacers.ReplaceAll(page, template);
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
			ThrowNull(parser, nameof(parser));
			ThrowNull(deletionText, nameof(deletionText));

			// Cheating and using text throughout, since this does not need to be parsed or acted upon currently, and is likely to be moved to another job soon anyway.
			var page = (Page)parser.Context;
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
				insertPos = parser.Nodes.Count;
				insertText = '\n' + deletionText;
			}
			else
			{
				insertText = deletionText + '\n';
			}

			parser.Nodes.Insert(insertPos, parser.Nodes.Factory.TextNode(insertText));
		}
		#endregion

		#region Private Methods
		private void FullEdit(object sender, Page page)
		{
			var parser = new ContextualParser(page);
			if (this.Replacements.TryGetValue(page, out var replacement) &&
				(replacement.Actions & ReplacementActions.NeedsEdited) != 0)
			{
				this.EditPageLoaded(parser, replacement);
			}

			this.BacklinkPageLoaded(parser);
			page.Text = parser.ToRaw();
		}

		private TitleCollection GetBacklinkTitles(PageCollection pageInfo)
		{
			var retval = new TitleCollection(this.Site);
			foreach (var replacement in this.Replacements)
			{
				if (pageInfo[replacement.From] is Page fromPage && (replacement.Actions & ReplacementActions.UpdateLinks) != 0)
				{
					foreach (var backlink in fromPage.Backlinks)
					{
						retval.Add(backlink.Key);
					}
				}
			}

			return retval;
		}

		private PageCollection LoadPageInfo()
		{
			var modules = this.PageInfoExtraModules | PageModules.Info;
			if ((this.FollowUpActions & FollowUpActions.AffectsBacklinks) != 0)
			{
				modules |= PageModules.Backlinks;
			}

			// Do not filter the From lists to only-existent pages, or backlinks and category members for non-existent pages will be missed.
			var fromTitles = new TitleCollection(this.Site);
			foreach (var replacement in this.Replacements)
			{
				fromTitles.Add(replacement.From);
			}

			var retval = PageCollection.Unlimited(this.Site, modules, false);
			retval.GetTitles(fromTitles);

			return retval;
		}

		private void ReadJsonFile()
		{
			var repFile = File.ReadAllText(this.replacementStatusFile);
			var reps = JsonConvert.DeserializeObject<IEnumerable<Replacement>>(repFile, this.titleConverter) ?? throw new InvalidOperationException();
			this.Replacements.AddRange(reps);
		}

		private void ValidateActions()
		{
			foreach (var replacement in this.Replacements)
			{
				if (replacement.Actions == ReplacementActions.None)
				{
					this.Warn($"Replacement Action for {replacement.From} is unknown.");
				}
			}
		}

		private void ValidateMoves()
		{
			var inPlaceMoves = false;
			var unique = new Dictionary<Title, Replacement>();
			foreach (var replacement in this.Replacements)
			{
				if ((replacement.Actions & ReplacementActions.Move) != 0 &&
					replacement.From.SimpleEquals(replacement.To))
				{
					this.Warn($"From and To pages cannot be the same: {replacement.From.FullPageName} = {replacement.To.FullPageName}");
					inPlaceMoves = true;
				}

				if (unique.TryGetValue(replacement.To, out var existing))
				{
					this.Warn("Duplicate To page. All related entries will be skipped.");
					this.Warn($"  Original: {existing.From.FullPageName} => {existing.To.FullPageName}");
					this.Warn($"  Second  : {replacement.From.FullPageName} => {replacement.To.FullPageName}");
					existing.Actions = ReplacementActions.Skip;
					existing.Reason = "duplicate To page";
					replacement.Actions = ReplacementActions.Skip;
					replacement.Reason = "duplicate To page";
				}
				else
				{
					unique.Add(replacement.To, replacement);
				}
			}

			if (inPlaceMoves)
			{
				throw new InvalidOperationException("Invalid page moves detected.");
			}
		}

		private void WriteJsonFile()
		{
			var newFile = JsonConvert.SerializeObject(this.Replacements, Formatting.Indented, this.titleConverter);
			File.WriteAllText(this.replacementStatusFile, newFile);
		}
		#endregion

		#region Private Classes
		private sealed class ReplacementCollection : KeyedCollection<Title, Replacement>
		{
			#region Constructors
			public ReplacementCollection()
				: base(SimpleTitleEqualityComparer.Instance)
			{
			}
			#endregion

			#region Public Methods
			public void Sort() => (this.Items as List<Replacement>)?.Sort((x, y) => SimpleTitleComparer.Instance.Compare(x.From, y.From));
			#endregion

			#region Protected Override Methods
			protected override Title GetKeyForItem(Replacement item) => item.From;
			#endregion
		}
		#endregion
	}
}
