namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using System.Text;
	using Newtonsoft.Json;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	#region Internal Enums
	[Flags]
	public enum FollowUpActions
	{
		CheckLinksRemaining = 1 << 0,
		EmitReport = 1 << 1,
		FixLinks = 1 << 2,
		FixCaption = 1 << 3,
		ProposeUnused = 1 << 4,
		Default = CheckLinksRemaining | EmitReport | FixCaption | FixLinks,
	}

	public enum MoveAction
	{
		None,
		RenameOnly,
		Move,
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

	public abstract class PageMoverJob : EditJob
	{
		#region Fields
		private readonly TitleCollection doNotDelete;
		private readonly ReplacementCollection replacements = new ReplacementCollection();
		private readonly ISimpleTitleJsonConverter titleConverter;
		private PageModules fromPageModules = PageModules.None;
		private string? logDetails = null;
		#endregion

		#region Constructors
		protected PageMoverJob(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.ProposedDeletions = new TitleCollection(site);
			this.doNotDelete = new TitleCollection(site);
			this.titleConverter = new ISimpleTitleJsonConverter(this.Site);
		}
		#endregion

		#region Public Properties
		public KeyedCollection<ISimpleTitle, Replacement> Replacements => this.replacements;
		#endregion

		#region Public Override Properties

		public override string LogDetails
		{
			get
			{
				if (this.logDetails == null)
				{
					var list = new List<string>();
					if (this.MoveAction == MoveAction.RenameOnly)
					{
						list.Add("rename only");
					}
					else if (this.MoveAction == MoveAction.None)
					{
						list.Add("do not move pages");
					}

					list.Add(
						this.RedirectOption == RedirectOption.Suppress ? "suppress redirects" :
						this.RedirectOption == RedirectOption.Create ? "create redirects" :
						"create redirects but propose them for deletion");

					if (this.MoveOverExisting)
					{
						list.Add("move over existing pages");
					}

					if (this.FollowUpActions.HasFlag(FollowUpActions.FixLinks))
					{
						list.Add("fix links");
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

		#region Protected Static Properties
		protected static string ReplacementStatusFile => Environment.ExpandEnvironmentVariables(@"%BotData%\Replacements.json");
		#endregion

		#region Protected Properties
		protected Action<Page, Replacement>? CustomEdit { get; set; }

		protected Action<ContextualParser>? CustomReplaceGeneral { get; set; }

		protected Action<ContextualParser, ISimpleTitle, ISimpleTitle>? CustomReplaceSpecific { get; set; }

		protected Dictionary<ISimpleTitle, Replacement> EditDictionary { get; } = new Dictionary<ISimpleTitle, Replacement>(SimpleTitleEqualityComparer.Instance);

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

		protected MoveAction MoveAction { get; set; } = MoveAction.Move;

		protected MoveOptions MoveOptions { get; set; } = MoveOptions.None;

		protected bool MoveOverExisting { get; set; } = false;

		protected TitleCollection ProposedDeletions { get; }

		protected RedirectOption RedirectOption { get; set; } = RedirectOption.Suppress;

		protected Action<Page, IWikiNode>? ReplaceSingleNode { get; set; }

		protected bool SuppressRedirects { get; set; } = true;

		protected Dictionary<string, Action<Page, TemplateNode>> TemplateReplacements { get; } = new Dictionary<string, Action<Page, TemplateNode>>(StringComparer.OrdinalIgnoreCase);

		protected PageModules ToPageModules { get; set; }
		#endregion

		#region Protected Static Methods
		protected static void DeleteFiles() => File.Delete(ReplacementStatusFile);
		#endregion

		#region Protected Methods

		protected void AddReplacement(string from, string to) => this.Replacements.Add(new Replacement(this.Site, from, to));

		protected void AddReplacement(Title from, Title to) => this.Replacements.Add(new Replacement(from, to));

		// [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Optiona, to be called only when necessary.")]
		protected IEnumerable<Replacement> LoadReplacementsFromFile(string fileName)
		{
			var repFile = File.ReadLines(fileName);
			var replacements = new List<Replacement>();
			foreach (var line in repFile)
			{
				var rep = line.Split('\t');
				replacements.Add(new Replacement(this.Site, rep[0].Trim(), rep[1].Trim()));
			}

			return replacements;
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
			var readFromFile = File.Exists(ReplacementStatusFile);
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

			DeleteFiles();
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
				if (replacement.Actions.HasFlag(ReplacementActions.Move))
				{
					actions.Add("move to " + replacement.To.AsLink());
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
				if (title.NamespaceId == MediaWikiNamespaces.Template && !title.PageName.EndsWith("/doc", StringComparison.OrdinalIgnoreCase))
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
			if (this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused) || this.FollowUpActions.HasFlag(FollowUpActions.FixLinks))
			{
				foreach (var replacement in this.replacements)
				{
					if (replacement.FromPage != null)
					{
						backlinkTitles.AddRange(replacement.FromPage.Backlinks.Keys);
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
			replacement.Reason = $"{replacement.To.AsLink()} exists";
		}

		protected virtual void MovePages()
		{
			var action = this.MoveAction switch
			{
				MoveAction.Move => "Moving pages",
				MoveAction.RenameOnly => "Renaming pages",
				_ => throw new InvalidOperationException() // We should never get here without having moves or renames to do.
			};

			this.StatusWriteLine(action);
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
						this.MoveOptions.HasFlag(MoveOptions.MoveTalkPage) && fromTitle.Namespace.TalkSpaceId != null,
						this.MoveOptions.HasFlag(MoveOptions.MoveSubPages) && fromTitle.Namespace.AllowsSubpages,
						this.RedirectOption == RedirectOption.Suppress);
					if (result.Value is IDictionary<string, string> values)
					{
						foreach (var item in values)
						{
							var from = new FullTitle(this.Site, item.Key);
							if (!from.SimpleEquals(replacement.From))
							{
								var to = new FullTitle(this.Site, item.Value);
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
				this.ReplaceSingleNode?.Invoke(page, node);
				switch (node)
				{
					case LinkNode link:
						// Formerly used for error reporting, but I think the page should be enough.
						// var source = (nodes is ContextualParser parser && parser.Title != null) ? parser.Title.FullPageName : "Unknown";
						this.UpdateLinkNode(page, link);
						break;
					case TagNode tag:
						if (tag.Name == "gallery")
						{
							this.UpdateGalleryLinks(page, tag);
						}

						break;
					case TemplateNode template:
						this.UpdateTemplateNode(page, template);
						break;
				}

				foreach (var subCollection in node.NodeCollections)
				{
					this.ReplaceNodes(page, subCollection);
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
					var changed = false;

					// Surround gallery link with actual link braces. Add a space in case line ends in an HTML link (parser cannot currently make sense of "[[File|[http link]]]").
					var link = SiteLink.FromGalleryText(this.Site, line + ' ');
					if (link.Text != null)
					{
						var captionParser = WikiTextParser.Parse(link.Text);
						this.ReplaceNodes(page, captionParser);
						var newText = WikiTextVisitor.Raw(captionParser);
						if (newText != link.Text)
						{
							link.Text = newText;
							changed = true;
						}
					}

					if (this.replacements.TryGetValue(link, out var replacement))
					{
						this.UpdateLinkText(page, link, replacement.To);

						var toTitle = replacement.To;
						if (toTitle.NamespaceId != MediaWikiNamespaces.File)
						{
							this.Warn("File to non-File move skipped due to being inside a gallery.");
						}
						else
						{
							// this.UpdateLinkText(page, link, toTitle);
							link.PageName = toTitle.PageName;
							changed = true;
						}
					}

					if (changed)
					{
						if (link.Coerced)
						{
							link.NamespaceId = MediaWikiNamespaces.Main;
						}

						newLine = link.ToString();
						newLine = newLine[2..^2].TrimEnd();
					}
				}

				sb.Append(newLine + '\n');
			}

			if (sb.Length > 0)
			{
				sb.Remove(sb.Length - 1, 1);
			}

			tag.InnerText = sb.ToString();
		}

		protected virtual void UpdateLinkNode(Page page, LinkNode link)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(link, nameof(link));

			var changed = false;
			var siteLink = SiteLink.FromLinkNode(this.Site, link);
			if (siteLink.Link != null && (new FullTitle(this.Site, siteLink.Link) is FullTitle linkLink) && this.replacements.TryGetValue(linkLink, out var linkReplacement))
			{
				changed = true;
				linkLink.NamespaceId = linkReplacement.To.NamespaceId;
				linkLink.PageName = linkReplacement.To.PageName;
				if (linkReplacement.To is IFullTitle full)
				{
					// Interwiki is always replaced; fragment is only replaced if not already specified. Might want to check replacement.From.Fragment if it's a full title as well, but this seems an unlikely scenario. Might even want to leave IFullTitle handling to a custom method when this gets redesigned to a visitor.
					linkLink.Interwiki = full.Interwiki;
					linkLink.Fragment ??= full.Fragment;
				}

				siteLink.Link = linkLink.ToString();
			}

			if (this.replacements.TryGetValue(siteLink, out var replacement))
			{
				changed = true;
				this.UpdateLinkText(page, siteLink, replacement.To);
				siteLink.NamespaceId = replacement.To.NamespaceId;
				siteLink.PageName = replacement.To.PageName;
				if (replacement.To is IFullTitle full)
				{
					// Interwiki is always replaced; fragment is only replaced if not already specified. Might want to check replacement.From.Fragment if it's a full title as well, but this seems an unlikely scenario. Might even want to leave IFullTitle handling to a custom method when this gets redesigned to a visitor.
					siteLink.Interwiki = full.Interwiki;
					siteLink.Fragment ??= full.Fragment;
				}
			}

			if (changed)
			{
				if (siteLink.ParametersDropped)
				{
					Debug.WriteLine($"{page.AsLink()}: Skipped update link because parameters were dropped in " + WikiTextVisitor.Raw(link));
				}
				else
				{
					var newLinkNode = siteLink.ToLinkNode();
					link.Title.Clear();
					link.Title.AddRange(newLinkNode.Title);
					link.Parameters.Clear();
					link.Parameters.AddRange(newLinkNode.Parameters);
				}
			}
		}

		[SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "False hit due to bug in Roslyn 2.9.8.")]
		protected virtual void UpdateLinkText(Page page, SiteLink link, ISimpleTitle toLink)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(toLink, nameof(toLink));
			ThrowNull(link, nameof(link));
			if (link.Text != null)
			{
				var captionParser = WikiTextParser.Parse(link.Text);
				this.ReplaceNodes(page, captionParser);
				link.Text = WikiTextVisitor.Raw(captionParser);
			}

			if (this.FollowUpActions.HasFlag(FollowUpActions.FixCaption))
			{
				if (link.Text != null)
				{
					var paramTitle = new FullTitle(this.Site, link.Text);
					if (paramTitle.SimpleEquals(link))
					{
						link.Text = toLink.FullPageName;
					}
					else if (link.PageNameEquals(paramTitle.PageName))
					{
						link.Text = toLink.PageName;
					}
				}
			}
			else if (string.IsNullOrEmpty(link.Text) && (link.LeadingColon || (link.NamespaceId != MediaWikiNamespaces.File && link.NamespaceId != MediaWikiNamespaces.Category)))
			{
				// If no link text exists, create some from the original title.
				link.Text = link.OriginalLink?.TrimStart(':');
			}
		}

		protected virtual void UpdateTemplateNode(Page page, TemplateNode template)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(template, nameof(template));

			var templateName = WikiTextVisitor.Value(template.Title);
			if (templateName.Length > 0)
			{
				var templateTitle = new FullTitle(this.Site, MediaWikiNamespaces.Template, templateName, false);
				if (this.replacements.TryGetValue(templateTitle, out var replacement))
				{
					var newTemplate = replacement.To;
					templateTitle.NamespaceId = newTemplate.NamespaceId;
					templateTitle.PageName = newTemplate.PageName;
					template.Title.Clear();
					var nameText = newTemplate.NamespaceId == MediaWikiNamespaces.Template ? newTemplate.PageName : newTemplate.FullPageName;
					template.Title.AddText(nameText);
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
					page.NamespaceId == MediaWikiNamespaces.Template ? "<noinclude>" + deletionText + "</noinclude>" :
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
			var parsedPage = ContextualParser.FromPage(page);
			this.ReplaceNodes(page, parsedPage);
			if (this.CustomReplaceSpecific != null)
			{
				foreach (var replacement in this.replacements)
				{
					this.CustomReplaceSpecific(parsedPage, replacement.From, replacement.To);
				}
			}

			this.CustomReplaceGeneral?.Invoke(parsedPage);
			page.Text = WikiTextVisitor.Raw(parsedPage);
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
				var noinclude = page.NamespaceId == MediaWikiNamespaces.Template;
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
			fromPages.RemoveNonExistent();

			var toPages = PageCollection.Unlimited(this.Site, this.ToPageModules, false); // Only worried about existence, so don't load anything other than that unless told to.
			if (!this.MoveOverExisting)
			{
				toPages.GetTitles(toTitles);
				toPages.RemoveNonExistent();
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
			var repFile = File.ReadAllText(ReplacementStatusFile);
			var reps = JsonConvert.DeserializeObject<IEnumerable<Replacement>>(repFile, this.titleConverter) ?? throw new InvalidOperationException();
			this.replacements.AddRange(reps);
		}

		private void WhatToDoMoves()
		{
			if (this.MoveAction == MoveAction.None)
			{
				return;
			}

			foreach (var replacement in this.replacements)
			{
				if (!replacement.Actions.HasFlag(ReplacementActions.Skip) && replacement.FromPage != null)
				{
					if (replacement.ToPage != null)
					{
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

			if (fromPage.NamespaceId == MediaWikiNamespaces.Category)
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
			File.WriteAllText(ReplacementStatusFile, newFile);
		}
		#endregion

		#region Private Classes
		private class ReplacementCollection : KeyedCollection<ISimpleTitle, Replacement>
		{
			public ReplacementCollection()
				: base(SimpleTitleEqualityComparer.Instance, 0)
			{
			}

			public IEnumerable<ISimpleTitle> Keys => this.Dictionary?.Keys ?? Array.Empty<ISimpleTitle>();

			public void Sort()
			{
				if (this.Items is List<Replacement> list)
				{
					list.Sort();
				}
			}

			protected override ISimpleTitle GetKeyForItem(Replacement item) => item.From;
		}
		#endregion
	}
}