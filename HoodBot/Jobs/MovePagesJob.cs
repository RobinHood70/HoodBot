namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.IO;
	using System.Text;
	using Newtonsoft.Json;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.ContextualParser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.BasicParser;
	using static RobinHood70.CommonCode.Globals;

	#region Internal Enums
	[Flags]
	public enum FollowUpActions
	{
		CheckLinksRemaining = 1 << 0,
		EmitReport = 1 << 1,
		FixLinks = 1 << 2,
		FixCaption = 1 << 3,
		ProposeUnused = 1 << 4,
		UpdateCategoryMembers = 1 << 5,
		Default = CheckLinksRemaining | EmitReport | FixCaption | FixLinks,
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

	public abstract class MovePagesJob : EditJob
	{
		#region Fields
		private readonly TitleCollection doNotDelete;
		private readonly ReplacementCollection replacements = new ReplacementCollection();
		private readonly string replacementStatusFile = Path.Combine(UespSite.GetBotFolder(), "Replacements.json");
		private readonly SimpleTitleJsonConverter titleConverter;
		private PageModules fromPageModules = PageModules.None;
		private string? logDetails;
		#endregion

		#region Constructors
		protected MovePagesJob(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.ProposedDeletions = new TitleCollection(site);
			this.doNotDelete = new TitleCollection(site);
			this.titleConverter = new SimpleTitleJsonConverter(this.Site);
		}
		#endregion

		#region Public Properties
		public KeyedCollection<Title, Replacement> Replacements => this.replacements;
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
		protected Action<Page, Replacement>? CustomEdit { get; set; }

		protected Action<Parser>? CustomReplaceGeneral { get; set; }

		protected Action<Parser, ISimpleTitle, ISimpleTitle>? CustomReplaceSpecific { get; set; }

		protected IDictionary<Title, Replacement> EditDictionary { get; } = new Dictionary<Title, Replacement>();

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

		protected MoveOptions MoveOptions { get; set; } = MoveOptions.None;

		protected TitleCollection ProposedDeletions { get; }

		protected RedirectOption RedirectOption { get; set; } = RedirectOption.Suppress;

		protected Action<Page, IWikiNode>? ReplaceSingleNode { get; set; }

		protected bool SuppressRedirects { get; set; } = true;

		protected IDictionary<string, Action<Page, TemplateNode>> TemplateReplacements { get; } = new Dictionary<string, Action<Page, TemplateNode>>(StringComparer.OrdinalIgnoreCase);

		protected PageModules ToPageModules { get; set; }
		#endregion

		#region Protected Methods
		protected void AddReplacement(string from, string to) => this.Replacements.Add(new Replacement(this.Site, from, to));

		protected void AddReplacement(Title from, Title to) => this.Replacements.Add(new Replacement(from, to));

		protected void DeleteFiles() => File.Delete(this.replacementStatusFile);

		// [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Optiona, to be called only when necessary.")]
		protected IEnumerable<Replacement> LoadReplacementsFromFile(string fileName)
		{
			var repFile = File.ReadLines(fileName);
			var replacementList = new List<Replacement>();
			foreach (var line in repFile)
			{
				var rep = line.Split(TextArrays.Tab);
				replacementList.Add(new Replacement(this.Site, rep[0].Trim(), rep[1].Trim()));
			}

			return replacementList;
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
			}

			this.replacements.Sort();
			this.GetReplacementInfo();
			if (!readFromFile)
			{
				this.StatusWriteLine("Figuring out what to do");
				this.WhatToDo();
				this.WriteJsonFile();
			}

			if (this.FollowUpActions.HasFlag(FollowUpActions.EmitReport))
			{
				this.EmitReport();
			}
		}

		protected override void Main()
		{
			if (this.MoveAction != MoveAction.None)
			{
				this.MovePages();
			}

			this.EditAfterMove();

			if (this.FollowUpActions.HasFlag(FollowUpActions.FixLinks))
			{
				this.FixLinks();
			}

			if (this.FollowUpActions.HasFlag(FollowUpActions.CheckLinksRemaining))
			{
				this.CheckRemaining();
			}

			this.DeleteFiles();
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void PopulateReplacements();
		#endregion

		#region Protected Virtual Methods
		protected virtual void CheckRemaining()
		{
			this.StatusWriteLine("Checking remaining pages");
			var leftovers = new TitleCollection(this.Site);
			var allBacklinks = PageCollection.Unlimited(this.Site, PageModules.Info | PageModules.Backlinks, false);
			allBacklinks.GetTitles(this.replacements.Keys);
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
			this.Progress = 0;
			foreach (var replacement in this.replacements)
			{
				if (replacement.Actions.HasFlag(ReplacementActions.Edit) || replacement.Actions.HasFlag(ReplacementActions.Propose))
				{
					var title = replacement.Actions.HasFlag(ReplacementActions.Move) && this.Site.EditingEnabled ? replacement.To : replacement.From;
					this.EditDictionary[title] = replacement; // In case something has added to the dictionary before we got here.
				}
			}

			var editPages = PageCollection.Unlimited(this.Site);
			editPages.PageLoaded += this.EditPageLoaded;
			editPages.GetTitles(this.EditDictionary.Keys);
			editPages.RemoveUnchanged();
			editPages.Sort();

			this.ProgressMaximum = editPages.Count + 1;
			this.Progress++;
			this.EditConflictAction = this.EditPageLoaded;
			foreach (var page in editPages)
			{
				var replacement = this.EditDictionary[page];
				this.SavePage(page, replacement.Actions.HasFlag(ReplacementActions.Propose) ? this.EditSummaryPropose : this.EditSummaryEditAfterMove, true);
				this.ProposedDeletions.Add(page);
				this.Progress++;
			}

			this.EditConflictAction = null;
		}

		protected virtual void EmitReport()
		{
			this.WriteLine("{| class=\"wikitable sortable\"");
			this.WriteLine("! Page !! Action");
			foreach (var replacement in this.replacements)
			{
				this.WriteLine("|-");
				this.Write(Invariant($"| {replacement.From} ([[Special:WhatLinksHere/{replacement.From}|links]]) || "));
				var actions = new List<string>();
				if (this.MoveAction != MoveAction.None && replacement.Actions.HasFlag(ReplacementActions.Move))
				{
					actions.Add("move to " + replacement.To.AsLink(false));
				}

				if (replacement.Actions.HasFlag(ReplacementActions.Edit) && this.CustomEdit != null)
				{
					actions.Add("edit" + (replacement.Actions.HasFlag(ReplacementActions.Move) ? " after move" : string.Empty));
				}

				if (replacement.Actions.HasFlag(ReplacementActions.Propose))
				{
					actions.Add("propose for deletion");
				}

				if (actions.Count == 0)
				{
					actions.Add("skip");
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

		protected virtual void FilterBacklinks(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			foreach (var title in this.Site.FilterPages)
			{
				backlinkTitles.Remove(title);
			}

			for (var i = backlinkTitles.Count - 1; i >= 0; i--)
			{
				var title = backlinkTitles[i];
				if (title.Namespace == MediaWikiNamespaces.Template && !title.PageName.EndsWith("/doc", StringComparison.OrdinalIgnoreCase))
				{
					backlinkTitles.RemoveAt(i);
				}
			}
		}

		protected virtual void FilterTalkLikePages(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			foreach (var catPage in this.Site.DiscussionPages)
			{
				backlinkTitles.Remove(catPage);
			}
		}

		protected virtual void FixLinks()
		{
			this.Progress = 0;
			var backlinkTitles = new TitleCollection(this.Site);
			if (this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused) || this.FollowUpActions.HasFlag(FollowUpActions.FixLinks) || this.FollowUpActions.HasFlag(FollowUpActions.UpdateCategoryMembers))
			{
				foreach (var replacement in this.replacements)
				{
					if (replacement.FromPage != null && replacement.Actions.HasFlag(ReplacementActions.Move))
					{
						backlinkTitles.AddRange(replacement.FromPage.Backlinks.Keys);
						if (this.FollowUpActions.HasFlag(FollowUpActions.UpdateCategoryMembers) && replacement.From.Namespace == MediaWikiNamespaces.Category && replacement.To.Namespace == MediaWikiNamespaces.Category)
						{
							var catMembers = new TitleCollection(this.Site);
							catMembers.GetCategoryMembers(replacement.From.FullPageName, CategoryMemberTypes.All, true);
							backlinkTitles.AddRange(catMembers);
						}
					}
				}
			}

			if (this.Site.EditingEnabled)
			{
				foreach (var replacement in this.replacements)
				{
					if (!replacement.Actions.HasFlag(ReplacementActions.Skip) && backlinkTitles.Contains(replacement.From))
					{
						// By the time we access backlinkTitles, the pages should already have been moved, so include the To title if there's overlap.
						backlinkTitles.Add(replacement.To);
						backlinkTitles.Remove(replacement.From);
					}
				}
			}

#if DEBUG
			backlinkTitles.Sort();
#endif

			backlinkTitles.RemoveNamespaces(
				MediaWikiNamespaces.Media,
				MediaWikiNamespaces.MediaWiki,
				MediaWikiNamespaces.Special);

			this.FilterBacklinks(backlinkTitles);

			// TODO: Merge with EditPages to avoid the possibility of editing a page twice.
			var backlinks = PageCollection.Unlimited(this.Site);
			backlinks.PageLoaded += this.BacklinkPageLoaded;
			backlinks.GetTitles(backlinkTitles);
			backlinks.PageLoaded -= this.BacklinkPageLoaded;
			backlinks.RemoveUnchanged();
			backlinks.Sort();

			this.ProgressMaximum = backlinks.Count + 1;
			this.Progress++;
			this.EditConflictAction = this.BacklinkPageLoaded;
			foreach (var page in backlinks)
			{
				this.SavePage(page, this.EditSummaryUpdateLinks, true);
				this.Progress++;
			}

			this.EditConflictAction = null;
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
			this.ProgressMaximum = this.replacements.Count;
			foreach (var replacement in this.replacements)
			{
				if (replacement.Actions.HasFlag(ReplacementActions.Move) && replacement.FromPage != null && replacement.FromPage.Exists)
				{
					if (!(replacement.From is Title fromTitle))
					{
						fromTitle = new Title(replacement.From);
					}

					var result = fromTitle.Move(
						replacement.To.FullPageName,
						this.EditSummaryMove,
						this.MoveOptions.HasFlag(MoveOptions.MoveTalkPage) && fromTitle.Namespace.TalkSpace != null,
						this.MoveOptions.HasFlag(MoveOptions.MoveSubPages) && fromTitle.Namespace.AllowsSubpages,
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
					this.replacements.Add(item);
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
			foreach (var node in nodes)
			{
				foreach (var subCollection in node.NodeCollections)
				{
					this.ReplaceNodes(page, subCollection);
				}

				this.ReplaceSingleNode?.Invoke(page, node);
				switch (node)
				{
					case LinkNode link:
						this.UpdateLinkNode(page, link);
						break;
					case TagNode tag:
						if (string.Equals(tag.Name, "gallery", StringComparison.Ordinal))
						{
							this.UpdateGalleryLinks(page, tag);
						}

						break;
					case TemplateNode template:
						this.UpdateTemplateNode(page, template);
						break;
				}
			}
		}

		protected virtual void UpdateGalleryLinks(Page page, TagNode tag)
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
					if (this.replacements.TryGetValue(link, out var replacement) && replacement.Actions.HasFlag(ReplacementActions.Move))
					{
						if (replacement.To.Namespace != MediaWikiNamespaces.File)
						{
							this.Warn("File to non-File move skipped due to being inside a gallery.");
							continue;
						}

						var newPageName = replacement.To.PageName;
						var newNamespace = (replacement.From.Namespace == replacement.To.Namespace && link.Coerced) ? this.Site[MediaWikiNamespaces.Main] : replacement.To.Namespace;
						var newLink = link.With(newNamespace, newPageName);
						this.UpdateLinkText(page, newLink, false);

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

		protected virtual void UpdateLinkNode(Page page, LinkNode node)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(node, nameof(node));

			var link = SiteLink.FromLinkNode(this.Site, node);
			if (this.replacements.TryGetValue(link, out var replacement) && replacement.Actions.HasFlag(ReplacementActions.Move))
			{
				link = link.With(replacement.To);
				this.UpdateLinkText(page, link, true);
				link.UpdateLinkNode(node);
			}
		}

		protected virtual void UpdateLinkText(Page page, SiteLink link, bool addCaption)
		{
			if (!this.FollowUpActions.HasFlag(FollowUpActions.FixCaption))
			{
				return;
			}

			ThrowNull(link, nameof(link));
			ThrowNull(page, nameof(page));
			if (link.Text == null)
			{
				if (addCaption && !page.IsRedirect && link.OriginalLink != null && (link.ForcedNamespaceLink || link.Namespace != MediaWikiNamespaces.Category))
				{
					// CONSIDER: For now, this is a simple check for all links on a redirect page. In theory, could/should only apply to the first link, in the uncommon case where there's additional text on the page.
					link.Text = link.OriginalLink.TrimStart(':');
				}
			}
			else
			{
				var textTitle = new TitleParser(this.Site, link.Text);
				if (textTitle.FullEquals(link))
				{
					link.Text = textTitle.ToString();
				}
				else if (textTitle.SimpleEquals(link))
				{
					var simp = new Title(textTitle);
					link.Text = simp.ToString();
				}
			}
		}

		protected virtual void UpdateTemplateNode(Page page, TemplateNode template)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(template, nameof(template));

			var templateName = WikiTextVisitor.Value(template.Title);
			if (templateName.Length > 0)
			{
				var templateTitle = FullTitle.Coerce(this.Site, MediaWikiNamespaces.Template, templateName);
				if (this.replacements.TryGetValue(templateTitle, out var replacement))
				{
					if (replacement.Actions.HasFlag(ReplacementActions.Move))
					{
						var newTemplate = replacement.To;
						template.Title.Clear();
						var nameText = newTemplate.Namespace == MediaWikiNamespaces.Template ? newTemplate.PageName : newTemplate.FullPageName;
						template.Title.AddText(nameText);
					}
				}
				else if (this.TemplateReplacements.TryGetValue(templateTitle.PageName, out var customTemplateAction))
				{
					var secondAction = customTemplateAction ?? throw new InvalidOperationException();
					secondAction?.Invoke(page, template);
				}
			}
		}

		protected virtual void WhatToDo()
		{
			this.WhatToDoMoves();
			foreach (var replacement in this.replacements)
			{
				if (this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused) && replacement.FromPage != null)
				{
					this.WhatToDoPropose(replacement);
				}

				if (replacement.Actions == ReplacementActions.None)
				{
					Debug.WriteLine($"Replacement Action for {replacement.From} is unknown.");
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
			while (status != ChangeStatus.Success && status != ChangeStatus.EditingDisabled)
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
		private void BacklinkPageLoaded(object sender, Page page)
		{
			// QUESTION: Is the below still an issue?
			// Page may not have been correctly found if it was recently moved. If it wasn't, there's little we can do here, so skip it and it'll show up in the report (assuming it's generated).
			// TODO: See if this can be worked around, like asking the wiki to purge and reload or something.
			var parsedPage = new Parser(page);
			this.ReplaceNodes(page, parsedPage.Nodes); // TODO: See if this can be re-written with ContextualParser methods.
			if (this.CustomReplaceSpecific != null)
			{
				foreach (var replacement in this.replacements)
				{
					this.CustomReplaceSpecific(parsedPage, replacement.From, replacement.To);
				}
			}

			this.CustomReplaceGeneral?.Invoke(parsedPage);
			page.Text = parsedPage.GetText();
		}

		private void EditPageLoaded(object sender, Page page)
		{
			var replacement = this.EditDictionary[page];
			if (
				replacement.FromPage != null &&
				replacement.FromPage.Exists &&
				this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused) &&
				replacement.Actions.HasFlag(ReplacementActions.Propose) &&
				!this.ProposedDeletions.Contains(page))
			{
				var deleteTemplate = TemplateNode.FromParts(
					"Proposeddeletion",
					("bot", "1"),
					(null, replacement.Reason ?? throw new InvalidOperationException()));
				var text = WikiTextVisitor.Raw(deleteTemplate);
				var noinclude = page.Namespace == MediaWikiNamespaces.Template;
				if (!noinclude && replacement.FromPage != null)
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

		private void GetReplacementInfo()
		{
			var fromTitles = new TitleCollection(this.Site);
			var toTitles = new TitleCollection(this.Site);
			foreach (var replacement in this.replacements)
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

			// TODO: Used to only load if proposing unused or fixing links. Removed since FromPage could be useful for custom moves. May want to reinstate with additional check for custom edit. Also check all code for where FromPage and ToPage are actually used.
			fromPages.GetTitles(fromTitles);
			fromPages.RemoveExists(false);

			var toPages = PageCollection.Unlimited(this.Site, this.ToPageModules, false); // Only worried about existence, so don't load anything other than that unless told to.
			if (this.MoveAction == MoveAction.MoveSafely)
			{
				toPages.GetTitles(toTitles);
				toPages.RemoveExists(false);
			}

			foreach (var replacement in this.replacements)
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
			this.replacements.AddRange(reps);
		}

		private void WhatToDoMoves()
		{
			foreach (var replacement in this.replacements)
			{
				if (!replacement.Actions.HasFlag(ReplacementActions.Skip) && replacement.FromPage != null)
				{
					if (this.MoveAction != MoveAction.None && replacement.ToPage != null)
					{
						replacement.Actions |= ReplacementActions.Skip;
						if (!this.ProposedDeletions.Contains(replacement.From))
						{
							this.HandleConflict(replacement);
						}
					}
					else
					{
						replacement.Actions |= ReplacementActions.Move;
						if (this.RedirectOption == RedirectOption.CreateButProposeDeletion && !replacement.FromPage.IsRedirect)
						{
							replacement.Actions |= ReplacementActions.Propose;
							replacement.Reason = "redirect from page move";
						}
					}
				}
			}
		}

		private void WhatToDoPropose(Replacement replacement)
		{
			var fromPage = replacement.FromPage;
			if (fromPage == null || fromPage.Backlinks.Count > 0)
			{
				return;
			}

			if (fromPage.Namespace == MediaWikiNamespaces.Category)
			{
				var catMembers = new TitleCollection(this.Site);
				catMembers.GetCategoryMembers(fromPage.PageName, CategoryMemberTypes.All, false);
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

		private void WriteJsonFile()
		{
			var newFile = JsonConvert.SerializeObject(this.replacements, Formatting.Indented, this.titleConverter);
			File.WriteAllText(this.replacementStatusFile, newFile);
		}
		#endregion

		#region private sealed classes
		private sealed class ReplacementCollection : KeyedCollection<Title, Replacement>
		{
			public IEnumerable<Title> Keys => this.Dictionary?.Keys ?? Array.Empty<Title>();

			public void Sort() => (this.Items as List<Replacement>)?.Sort();

			protected override Title GetKeyForItem(Replacement item) => item.From;
		}
		#endregion
	}
}