namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
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
		CheckLinksRemaining = 1 << 0,
		EmitReport = 1 << 1,
		FixLinks = 1 << 2,
		UpdateCaption = 1 << 3,
		ProposeUnused = 1 << 4,
		UpdateCategoryMembers = 1 << 5,
		Default = CheckLinksRemaining | EmitReport | FixLinks,
	}

	public enum MoveAction
	{
		None,
		MoveSafely,
		MoveOverExisting,
	}

	/* Not currently supported.
	[Flags]
	public enum MoveOptions
	{
		None = 0,
		MoveSubPages = 1 << 1,
		MoveTalkPage = 1 << 2
	}
	*/

	public enum RedirectOption
	{
		Suppress,
		Create,
		CreateButProposeDeletion
	}
	#endregion

	/// <summary>Underlying job to move pages. This partial class contains the job logic, while the other one contains the parameter replacer methods.</summary>
	/// <seealso cref="EditJob" />
	public abstract class MovePagesJob : EditJob
	{
		#region Fields
		private readonly TitleCollection doNotDelete;
		private readonly ReplacementCollection editReplacements;
		private readonly string replacementStatusFile;
		private readonly SimpleTitleJsonConverter titleConverter;
		private readonly ParameterReplacers parameterReplacers;
		private PageModules fromPageModules = PageModules.None;
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
			this.ProposedDeletions = new(this.Site);
			this.BacklinkTitles = new(this.Site);
			this.doNotDelete = new(this.Site);
			this.editReplacements = new ReplacementCollection(!this.Site.EditingEnabled);
			this.parameterReplacers = new ParameterReplacers(this);
			this.titleConverter = new(this.Site);
			replacementName = replacementName == null ? string.Empty : " - " + replacementName;
			this.replacementStatusFile = UespSite.GetBotDataFolder($"Replacements{replacementName}.json");
		}
		#endregion

		#region Public Properties
		public ReplacementCollection Replacements { get; } = new ReplacementCollection();
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

					list.Add(
						this.RedirectOption == RedirectOption.Suppress ? "suppress redirects" :
						this.RedirectOption == RedirectOption.Create ? "create redirects" :
						"create redirects but propose them for deletion");

					if (this.FollowUpActions.HasFlag(FollowUpActions.FixLinks))
					{
						list.Add("fix links");
					}

					if (this.FollowUpActions.HasFlag(FollowUpActions.UpdateCategoryMembers))
					{
						list.Add("re-categorize category members");
					}

					if (this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused))
					{
						list.Add("propose unused pages for deletion");
					}

					if (this.FollowUpActions.HasFlag(FollowUpActions.CheckLinksRemaining))
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
		protected TitleCollection BacklinkTitles { get; }

		protected Action<Page, Replacement>? CustomEdit { get; set; }

		protected Action<ContextualParser>? CustomReplace { get; set; }

		protected string EditSummaryEditAfterMove { get; set; } = "Update text after page move";

		protected string EditSummaryMove { get; set; } = "Rename";

		protected string EditSummaryPropose { get; set; } = "Propose for deletion";

		protected string EditSummaryUpdateLinks { get; set; } = "Update links after page move";

		protected FollowUpActions FollowUpActions { get; set; } = FollowUpActions.Default;

		protected PageModules FromPageModules
		{
			get => this.fromPageModules | PageModules.Info | PageModules.Backlinks;
			set => this.fromPageModules = value;
		}

		protected MoveAction MoveAction { get; set; } = MoveAction.MoveSafely;

		//// protected MoveOptions MoveOptions { get; set; } = MoveOptions.None;

		protected TitleCollection ProposedDeletions { get; }

		protected bool RecursiveCategoryMembers { get; set; } = true;

		protected RedirectOption RedirectOption { get; set; } = RedirectOption.Suppress;

		protected Action<Page, IWikiNode>? ReplaceSingleNode { get; set; }

		protected bool SuppressRedirects { get; set; } = true;

		protected PageModules ToPageModules { get; set; }
		#endregion

		#region Protected Methods
		protected void AddReplacement(string from, string to) => this.Replacements.Add(new Replacement(this.Site, from, to));

		protected void AddReplacement(Title from, Title to) => this.Replacements.Add(new Replacement(from, to));

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
			this.ProposedDeletions.AddRange(this.LoadProposedDeletions());
			foreach (var template in this.Site.DeletePreventionTemplates)
			{
				this.doNotDelete.GetBacklinks(template.FullPageName, BacklinksTypes.EmbeddedIn, true);
			}

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
					var inPlaceMoves = false;
					var unique = new Dictionary<Title, Replacement>();
					foreach (var replacement in this.Replacements)
					{
						if (replacement.From == replacement.To)
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
			}

			this.Replacements.Sort();
			this.GetReplacementInfo();
			if (!readFromFile)
			{
				this.StatusWriteLine("Figuring out what to do");
				this.WhatToDo();
				this.WriteJsonFile();
				var simple = new List<string>();
				/*
				foreach (var replacement in this.Replacements)
				{
					simple.Add($"{replacement.From}\t{replacement.To}");
				}

				File.WriteAllLines(@"D:\Data\HoodBot\FullList.txt", simple); */
			}

			if (this.FollowUpActions.HasFlag(FollowUpActions.EmitReport))
			{
				this.EmitReport();
			}

			if (this.FollowUpActions.HasFlag(FollowUpActions.FixLinks))
			{
				// This can be long, so we do it beforehand and save it, rather than showing a lengthy pause between moving pages and fixing links. This will also potentially allow a merger with this.editDictionary later.
				this.GetBacklinkTitles();
			}

			foreach (var replacement in this.Replacements)
			{
				if (replacement.Actions.HasFlag(ReplacementActions.Edit) || replacement.Actions.HasFlag(ReplacementActions.Propose))
				{
					this.editReplacements.Add(replacement);
				}
			}
		}

		protected override void Main()
		{
			if (this.MoveAction != MoveAction.None)
			{
				this.MovePages();
			}

			if (this.editReplacements.Count > 0)
			{
				this.EditAfterMove();
			}

			if (this.FollowUpActions.HasFlag(FollowUpActions.FixLinks))
			{
				this.FixLinks();
			}

			if (this.FollowUpActions.HasFlag(FollowUpActions.CheckLinksRemaining))
			{
				this.CheckRemaining();
			}

			this.DeleteStatusFile();
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void PopulateReplacements();
		#endregion

		#region Protected Virtual Methods
		protected virtual void BacklinkPageLoaded(object sender, Page page)
		{
			// QUESTION: Is the below still an issue?
			// Page may not have been correctly found if it was recently moved. If it wasn't, there's little we can do here, so skip it and it'll show up in the report (assuming it's generated).
			// TODO: See if this can be worked around, like asking the wiki to purge and reload or something.
			var parsedPage = new ContextualParser(page);
			this.ReplaceNodes(page, parsedPage.Nodes); // TODO: See if this can be re-written with ContextualParser methods.
			this.CustomReplace?.Invoke(parsedPage);
			page.Text = parsedPage.ToRaw();
		}

		protected virtual void CheckRemaining()
		{
			this.StatusWriteLine("Checking remaining pages");
			var leftovers = new TitleCollection(this.Site);
			var allBacklinks = PageCollection.Unlimited(this.Site, PageModules.Info | PageModules.Backlinks, false);
			allBacklinks.GetTitles(this.Replacements.Keys);
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

		protected virtual void EditAfterMove()
		{
			var editPages = PageCollection.Unlimited(this.Site);
			editPages.PageLoaded += this.EditPageLoaded;
			editPages.GetTitles(this.editReplacements.Keys);
			editPages.RemoveUnchanged();
			editPages.Sort();

			this.ResetProgress(editPages.Count);
			this.EditConflictAction = this.EditPageLoaded;
			foreach (var page in editPages)
			{
				var replacement = this.editReplacements[page];
				this.SavePage(page, replacement.Actions.HasFlag(ReplacementActions.Propose) ? this.EditSummaryPropose : this.EditSummaryEditAfterMove, true);
				this.ProposedDeletions.Add(page);
				this.Progress++;
			}

			this.EditConflictAction = null;
		}

		protected virtual void EditPageLoaded(object sender, Page page)
		{
			var replacement = this.editReplacements[page];
			if (
				replacement.FromPage != null &&
				replacement.FromPage.Exists &&
				this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused) &&
				replacement.Actions.HasFlag(ReplacementActions.Propose) &&
				!this.ProposedDeletions.Contains(page))
			{
				ThrowNull(replacement.Reason, nameof(replacement), nameof(replacement.Reason));
				var text = "{{Proposeddeletion|bot=1|" + replacement.Reason + "}}";
				var noinclude = page.Namespace == MediaWikiNamespaces.Template;
				if (!noinclude)
				{
					foreach (var backlink in replacement.FromPage.Backlinks)
					{
						if (backlink.Value == BacklinksTypes.EmbeddedIn)
						{
							noinclude = true;
							break;
						}
					}
				}

				page.Text =
					noinclude ? "<noinclude>" + text + "</noinclude>" :
					page.IsRedirect ? page.Text + '\n' + text :
					text + '\n' + page.Text;
			}

			if (replacement.Actions.HasFlag(ReplacementActions.Edit) && this.CustomEdit != null)
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
					if (replacement.Actions.HasFlag(ReplacementActions.Move))
					{
						actions.Add("move to " + replacement.To.AsLink(false));
					}
				}
				else if (replacement.Actions.HasFlag(ReplacementActions.UpdateLinks))
				{
					actions.Add("update links to " + replacement.To.AsLink(false));
				}

				if (replacement.Actions.HasFlag(ReplacementActions.Edit) && this.CustomEdit != null)
				{
					actions.Add("edit" + (replacement.Actions.HasFlag(ReplacementActions.Move) ? " after move" : string.Empty));
				}

				if (replacement.Actions.HasFlag(ReplacementActions.Propose))
				{
					actions.Add("propose for deletion");
				}

				if (replacement.Actions.HasFlag(ReplacementActions.Skip))
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

		protected virtual void FilterDiscussionPages()
		{
			foreach (var title in this.Site.DiscussionPages)
			{
				this.BacklinkTitles.Remove(title);
			}
		}

		protected virtual void FixLinks()
		{
			// TODO: Merge with EditPages to avoid the possibility of editing a page twice.
			var backlinks = PageCollection.Unlimited(this.Site);
			backlinks.PageLoaded += this.BacklinkPageLoaded;
			backlinks.GetTitles(this.BacklinkTitles);
			backlinks.PageLoaded -= this.BacklinkPageLoaded;
			backlinks.RemoveUnchanged();
			backlinks.Sort();

			this.StatusWriteLine("Updating links");
			this.ResetProgress(backlinks.Count);
			this.EditConflictAction = this.BacklinkPageLoaded;
			foreach (var page in backlinks)
			{
				this.SavePage(page, this.EditSummaryUpdateLinks, true);
				this.Progress++;
			}

			this.EditConflictAction = null;
		}

		protected virtual void GetBacklinkTitles()
		{
			if (this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused) || this.FollowUpActions.HasFlag(FollowUpActions.FixLinks) || this.FollowUpActions.HasFlag(FollowUpActions.UpdateCategoryMembers))
			{
				foreach (var replacement in this.Replacements)
				{
					if (replacement.FromPage != null && replacement.Actions.HasFlag(ReplacementActions.UpdateLinks))
					{
						this.BacklinkTitles.AddRange(replacement.FromPage.Backlinks.Keys);
					}
				}

				if (this.FollowUpActions.HasFlag(FollowUpActions.UpdateCategoryMembers))
				{
					this.StatusWriteLine("Getting category members");
					this.GetCategoryMembers();
				}
			}

			foreach (var replacement in this.Replacements)
			{
				if (replacement.Actions.HasFlag(ReplacementActions.UpdateLinks) && this.BacklinkTitles.Contains(replacement.From))
				{
					// Ensures that pages referenced in backlinks are checked even if they were moved. Also keep To page to fix double-redirects and any other case where the From page might sitll contain a reference.
					this.BacklinkTitles.Add(replacement.To);
				}
			}

#if DEBUG
			this.BacklinkTitles.Sort();
#endif

			this.FilterBacklinkTitles();
		}

		protected virtual void GetCategoryMembers()
		{
			this.ResetProgress(this.Replacements.Count);
			foreach (var replacement in this.Replacements)
			{
				if (replacement.Actions.HasFlag(ReplacementActions.UpdateLinks) && replacement.FromPage != null
					&& replacement.From.Namespace == MediaWikiNamespaces.Category
					&& replacement.To.Namespace == MediaWikiNamespaces.Category)
				{
					var catMembers = new TitleCollection(this.Site);
					catMembers.GetCategoryMembers(replacement.From.FullPageName, CategoryMemberTypes.All, this.RecursiveCategoryMembers);
					this.BacklinkTitles.AddRange(catMembers);
				}

				this.Progress++;
			}
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
			var toAdd = new ReplacementCollection();
			this.Progress = 0;
			this.ProgressMaximum = this.Replacements.Count;
			foreach (var replacement in this.Replacements)
			{
				if (replacement.Actions.HasFlag(ReplacementActions.Move) && replacement.FromPage != null && replacement.FromPage.Exists)
				{
					if (replacement.From is not Title fromTitle)
					{
						fromTitle = new Title(replacement.From);
					}

					var result = fromTitle.Move(
						replacement.To.FullPageName,
						this.EditSummaryMove,
						false /* this.MoveOptions.HasFlag(MoveOptions.MoveTalkPage) && fromTitle.Namespace.TalkSpace != null */,
						false /* this.MoveOptions.HasFlag(MoveOptions.MoveSubPages) && fromTitle.Namespace.AllowsSubpages */,
						this.RedirectOption == RedirectOption.Suppress);
					if (result.Value is IDictionary<string, string> values)
					{
						foreach (var item in values)
						{
							var from = FullTitle.FromName(this.Site, item.Key);
							if (!from.SimpleEquals(replacement.From))
							{
								var to = FullTitle.FromName(this.Site, item.Value);
								var newReplacement = new Replacement(from, to)
								{
									Actions = replacement.Actions,
									Reason = replacement.Reason,
								};

								toAdd.Add(newReplacement);
							}
						}
					}
				}

				this.Progress++;
			}

			if (toAdd.Count > 0)
			{
				foreach (var item in toAdd)
				{
					this.Replacements.Add(item);
				}

				this.GetReplacementInfo();
				this.WriteJsonFile(); // TODO: Re-writing the file down here isn't fully stop/resume proof. In theory, it should be re-written for every move, but that has the issue of needing to add directly to the replacements we're looping through. Copy all replacements and loop through that, maybe? Or have a separate file for additions that'll get picked up on re-start?
			}
		}

		protected virtual void ReplaceNodes(Page page, NodeCollection nodes)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(nodes, nameof(nodes));

			// Possibly better as a visitor class?
			var isFirstLink = true;
			foreach (var node in nodes)
			{
				if (node is IParentNode parent)
				{
					foreach (var subCollection in parent.NodeCollections)
					{
						this.ReplaceNodes(page, subCollection);
					}
				}

				this.ReplaceSingleNode?.Invoke(page, node);
				switch (node)
				{
					case ITagNode tag:
						if (string.Equals(tag.Name, "gallery", StringComparison.Ordinal))
						{
							this.UpdateGalleryLinks(page, tag);
						}

						break;
					case SiteLinkNode link:
						this.UpdateLinkNode(page, link, page.IsRedirect && isFirstLink);
						isFirstLink = false;
						break;
					case SiteTemplateNode template:
						this.UpdateTemplateNode(page, template);
						break;
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
						&& replacement.Actions.HasFlag(ReplacementActions.UpdateLinks))
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
				&& replacement.Actions.HasFlag(ReplacementActions.UpdateLinks)
				&& (link.ForcedNamespaceLink
					|| link.Namespace != MediaWikiNamespaces.Category
					|| replacement.To.Namespace != MediaWikiNamespaces.Category
					|| this.FollowUpActions.HasFlag(FollowUpActions.UpdateCategoryMembers)))
			{
				link = link.With(replacement.To);
				this.UpdateLinkText(page, replacement.From, link, !isRedirectTarget);
				link.UpdateLinkNode(node);
			}
		}

		protected virtual void UpdateLinkText(Page page, Title oldTitle, SiteLink newLink, bool addCaption)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(oldTitle, nameof(oldTitle));
			ThrowNull(newLink, nameof(newLink));
			if (this.FollowUpActions.HasFlag(FollowUpActions.UpdateCaption))
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
						var simp = new Title(textTitle);
						newLink.Text = simp.ToString();
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
				replacement.Actions.HasFlag(ReplacementActions.UpdateLinks))
			{
				var newTemplate = replacement.To;
				var nameText = newTemplate.Namespace == MediaWikiNamespaces.Template ? newTemplate.PageName : newTemplate.FullPageName;
				template.SetTitle(nameText);
			}

			this.parameterReplacers.ReplaceAll(page, template);
		}

		protected virtual void WhatToDo()
		{
			this.WhatToDoMoves();
			foreach (var replacement in this.Replacements)
			{
				if (this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused))
				{
					this.WhatToDoPropose(replacement);
				}

				if (replacement.Actions == ReplacementActions.None)
				{
					this.Warn($"Replacement Action for {replacement.From} is unknown.");
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static void ProposeForDeletion(Page page, string deletionText)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(deletionText, nameof(deletionText));
			var status = ChangeStatus.Unknown;
			while (status is not ChangeStatus.Success and not ChangeStatus.EditingDisabled)
			{
				page.Text =
					page.Namespace == MediaWikiNamespaces.Template ? "<noinclude>" + deletionText + "</noinclude>" :
					page.IsRedirect ? page.Text + '\n' + deletionText :
					deletionText + '\n' + page.Text;
				status = page.Save("Propose for deletion", false);
			}
		}
		#endregion

		#region Private Methods
		private void FilterBacklinkTitles()
		{
			this.BacklinkTitles.RemoveNamespaces(
				MediaWikiNamespaces.Media,
				MediaWikiNamespaces.MediaWiki,
				MediaWikiNamespaces.Special);

			foreach (var title in this.Site.FilterPages)
			{
				this.BacklinkTitles.Remove(title);
			}

			this.FilterTemplatesExceptDocs();
		}

		private void FilterTemplatesExceptDocs()
		{
			for (var i = this.BacklinkTitles.Count - 1; i >= 0; i--)
			{
				if (this.BacklinkTitles[i] is Title title &&
					title.Namespace == MediaWikiNamespaces.Template &&
					!title.PageName.EndsWith("/doc", StringComparison.OrdinalIgnoreCase))
				{
					this.BacklinkTitles.RemoveAt(i);
				}
			}
		}

		private void GetReplacementInfo()
		{
			var fromTitles = new TitleCollection(this.Site);
			var toTitles = new TitleCollection(this.Site);
			foreach (var replacement in this.Replacements)
			{
				if (replacement.FromPage == null)
				{
					fromTitles.Add(replacement.From);
				}

				if (replacement.ToPage == null)
				{
					toTitles.Add(replacement.To);
				}
			}

			var fromPages = PageCollection.Unlimited(this.Site, this.FromPageModules, false);

			// Do not filter this to only-existent pages or backlinks and category members for non-existent pages will be missed.
			// TODO: Used to only load if proposing unused or fixing links. Removed since FromPage could be useful for custom moves. May want to reinstate with additional check for custom edit. Also check all code for where FromPage and ToPage are actually used.
			fromPages.GetTitles(fromTitles);

			var toPages = PageCollection.Unlimited(this.Site, this.ToPageModules, false); // Only worried about existence, so don't load anything other than that unless told to.
			if (this.MoveAction == MoveAction.MoveSafely)
			{
				toPages.GetTitles(toTitles);
				toPages.RemoveExists(false);
			}

			foreach (var replacement in this.Replacements)
			{
				if (fromPages.TryGetValue(replacement.From, out var fromPage))
				{
					replacement.FromPage = fromPage;
				}

				if (toPages.TryGetValue(replacement.To, out var toPage))
				{
					replacement.ToPage = toPage;
				}
			}
		}

		private void ReadJsonFile()
		{
			var repFile = File.ReadAllText(this.replacementStatusFile);
			var reps = JsonConvert.DeserializeObject<IEnumerable<Replacement>>(repFile, this.titleConverter) ?? throw new InvalidOperationException();
			this.Replacements.AddRange(reps);
		}

		private void WhatToDoMoves()
		{
			foreach (var replacement in this.Replacements)
			{
				if (!replacement.Actions.HasFlag(ReplacementActions.Skip))
				{
					if (replacement.FromPage == null)
					{
						// From title was uninterpretable
						this.Warn($"FromPage for {replacement.From} is null!");
						replacement.Actions = ReplacementActions.Skip;
						replacement.Reason = "page title is indecipherable.";
					}
					else if (this.MoveAction == MoveAction.None)
					{
						replacement.Actions |= ReplacementActions.UpdateLinks;
					}
					else
					{
						// Any proposed move.
						if (!replacement.FromPage.Exists)
						{
							// From title does not exist.
							replacement.Actions = ReplacementActions.Skip;
							replacement.Reason = "page doesn't exist";
						}
						else if (replacement.ToPage?.Exists == true)
						{
							replacement.Actions = ReplacementActions.Skip;
							if (!this.ProposedDeletions.Contains(replacement.From))
							{
								this.HandleConflict(replacement);
							}

							// HandleConflict may have resolved the issue and changed the flag, so check again.
							if (replacement.Actions.HasFlag(ReplacementActions.Skip))
							{
								replacement.Reason ??= $"To page exists";
							}
						}
						else
						{
							replacement.Actions |= ReplacementActions.Move | ReplacementActions.UpdateLinks;
							if (this.RedirectOption == RedirectOption.CreateButProposeDeletion && !replacement.FromPage.IsRedirect)
							{
								replacement.Actions |= ReplacementActions.Propose;
								replacement.Reason = "redirect from page move";
							}
						}
					}
				}
			}
		}

		private void WhatToDoPropose(Replacement replacement)
		{
			var fromPage = replacement.FromPage;
			if (fromPage != null && fromPage.Exists && fromPage.Backlinks.Count <= 0)
			{
				if (fromPage.Namespace == MediaWikiNamespaces.Category)
				{
					var catMembers = new TitleCollection(this.Site);
					catMembers.GetCategoryMembers(fromPage.FullPageName, CategoryMemberTypes.All, false);
					if (catMembers.Count > 0)
					{
						return;
					}
				}

				if (this.doNotDelete.Contains(fromPage))
				{
					if (this.MoveAction != MoveAction.None)
					{
						replacement.Actions |= ReplacementActions.Move;
						replacement.Reason = "no links, but marked to not be deleted";
					}
				}
				else if (this.ProposedDeletions.Contains(fromPage))
				{
					replacement.Actions = ReplacementActions.Skip;
					replacement.Reason = "already proposed for deletion";
				}
				else
				{
					replacement.Actions |= ReplacementActions.Propose;
					replacement.Reason = "unused";
				}
			}
		}

		private void WriteJsonFile()
		{
			var newFile = JsonConvert.SerializeObject(this.Replacements, Formatting.Indented, this.titleConverter);
			File.WriteAllText(this.replacementStatusFile, newFile);
		}
		#endregion
	}
}