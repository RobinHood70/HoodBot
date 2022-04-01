namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
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
	using RobinHood70.WikiCommon.Properties;

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
		private readonly ReplacementCollection replacements = new();
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
			this.parameterReplacers = new ParameterReplacers(this, this.replacements);
			this.titleConverter = new(this.Site);
			replacementName = replacementName == null ? string.Empty : " - " + replacementName;
			this.replacementStatusFile = UespSite.GetBotDataFolder($"Replacements{replacementName}.json");
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
						var value = this.RedirectOption switch
						{
							RedirectOption.Suppress => "suppress redirects",
							RedirectOption.Create => "create redirects",
							RedirectOption.CreateButProposeDeletion => "create redirects but propose them for deletion",
							_ => throw new InvalidOperationException(GlobalMessages.InvalidSwitchValue)
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
			CreateTitle.FromUnvalidated(this.Site, from),
			CreateTitle.FromUnvalidated(this.Site, to));

		protected void AddReplacement(Title from, Title to) => this.replacements.Add(new Replacement(from, to));

		protected void AddReplacement(string from, string to, ReplacementActions initialActions) => this.AddReplacement(
			CreateTitle.FromUnvalidated(this.Site, from),
			CreateTitle.FromUnvalidated(this.Site, to),
			initialActions);

		protected void AddReplacement(Title from, Title to, ReplacementActions initialActions) => this.replacements.Add(new Replacement(from, to, new Replacement.DetailedActions(initialActions)));

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

			this.replacements.Sort();
			var fromPages = this.GetFromPages();
			this.GetPageActions(fromPages);
			var categoryMembers = this.GetCategoryMembers(fromPages);
			var loadTitles = this.GetLoadTitles(fromPages, categoryMembers);
			if (!readFromFile)
			{
				this.StatusWriteLine("Figuring out what to do");
				this.SetupProposedDeletions(fromPages, categoryMembers);
				this.ValidateActions();
				this.WriteJsonFile();
			}

			if ((this.FollowUpActions & FollowUpActions.EmitReport) != 0)
			{
				this.EmitReport();
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
		protected virtual void BacklinkPageLoaded(ContextualParser parser) => this.ReplaceBacklinks(parser.NotNull().Page, parser); // TODO: See if this can be re-written with ContextualParser methods.

		protected virtual void CheckRemaining()
		{
			this.StatusWriteLine("Checking remaining pages");
			TitleCollection leftovers = new(this.Site);
			PageCollection allBacklinks = PageCollection.Unlimited(this.Site, PageModules.Info | PageModules.Backlinks, false);
			TitleCollection backlinkTitles = new(this.Site);
			foreach (var item in this.replacements)
			{
				backlinkTitles.Add(item.From);
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

		protected virtual void CustomEdit(Page page, Replacement replacement)
		{
		}

		protected virtual void EditPageLoaded(ContextualParser parser, Replacement replacement)
		{
			var page = parser.Page;
			var moveActions = replacement.MoveActions.NotNull(nameof(replacement), nameof(replacement.MoveActions));
			if (page.Exists &&
				(this.FollowUpActions & FollowUpActions.ProposeUnused) != 0 &&
				moveActions.HasAction(ReplacementActions.Propose))
			{
				ProposeForDeletion(parser, "{{Proposeddeletion|bot=1|" + moveActions.Reason.UpperFirst(this.Site.Culture) + "}}");
				this.SaveInfo[replacement.From] = new SaveInfo(this.EditSummaryPropose, false);
			}

			if (moveActions.HasAction(ReplacementActions.Edit))
			{
				this.CustomEdit(page, replacement);
			}
		}

		protected virtual void EmitReport()
		{
			this.WriteLine("{| class=\"wikitable sortable\"");
			this.WriteLine("! Page !! Action");
			foreach (var replacement in this.replacements)
			{
				this.WriteLine("|-");
				this.Write(FormattableString.Invariant($"| {replacement.From} ([[Special:WhatLinksHere/{replacement.From}|links]]) || "));
				var actions = replacement.MoveActions.NotNull(nameof(replacement), nameof(replacement.MoveActions)).Actions;
				List<string> actionsList = new();
				if (this.MoveAction == MoveAction.None)
				{
					if ((actions & ReplacementActions.UpdateLinks) != 0)
					{
						actionsList.Add("update links to " + replacement.To.AsLink());
					}
				}
				else if ((actions & ReplacementActions.Move) != 0)
				{
					actionsList.Add("move to " + replacement.To.AsLink());
				}

				if ((actions & ReplacementActions.Edit) != 0)
				{
					actionsList.Add("edit" + (replacement.MoveActions.HasAction(ReplacementActions.Move) ? " moved page" : string.Empty));
				}

				if ((actions & ReplacementActions.Propose) != 0)
				{
					actionsList.Add("propose for deletion");
				}

				if (replacement.MoveActions.HasAction(ReplacementActions.Skip))
				{
					actionsList.Add("skip");
				}

				if (actionsList.Count == 0)
				{
					actionsList.Add("none");
				}

				var action = string.Join(", ", actionsList).UpperFirst(this.Site.Culture);
				if (replacement.MoveActions.Reason != null)
				{
					action += " (" + replacement.MoveActions.Reason + ")";
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

		protected virtual IReadOnlyDictionary<Title, TitleCollection> GetCategoryMembers(PageCollection fromPages)
		{
			this.StatusWriteLine("Getting category members");
			Dictionary<Title, TitleCollection> retval = new();
			if ((this.FollowUpActions & FollowUpActions.NeedsCategoryMembers) != 0)
			{
				var skipCats = (this.FollowUpActions & FollowUpActions.NeedsCategoryMembers) == 0;
				var categoryReplacements = new List<Replacement>(this.replacements).FindAll(replacement => replacement.From.Namespace == MediaWikiNamespaces.Category);
				if (skipCats || categoryReplacements.Count == 0)
				{
					return ImmutableDictionary<Title, TitleCollection>.Empty;
				}

				this.ResetProgress(categoryReplacements.Count);
				foreach (var replacement in categoryReplacements)
				{
					if ((this.FollowUpActions & FollowUpActions.ProposeUnused) != 0 ||
						(replacement.MoveActions.HasAction(ReplacementActions.UpdateLinks) &&
						replacement.From.Namespace == replacement.To.Namespace))
					{
						TitleCollection catMembers = new(this.Site);
						catMembers.GetCategoryMembers(replacement.From.PageName, CategoryMemberTypes.All, this.RecursiveCategoryMembers);
						if (catMembers.Count > 0)
						{
							retval.Add(replacement.From, catMembers);
						}

						this.Progress++;
					}
				}
			}

			return retval;
		}

		protected virtual Replacement.DetailedActions HandleConflict(Replacement replacement) => new(
			replacement.MoveActions.Actions & ~ReplacementActions.Move,
			$"{replacement.To.AsLink()} exists");

		protected virtual void MovePages()
		{
			this.StatusWriteLine("Moving pages");
			this.Progress = 0;
			this.ProgressMaximum = this.replacements.Count;
			var moveReplacements = new List<Replacement>(this.replacements).FindAll(replacement => replacement.MoveActions.HasAction(ReplacementActions.Move));
			foreach (var replacement in moveReplacements)
			{
				var fromTitle = replacement.From;
				var moveTalkPage = (this.MoveExtra & MoveOptions.MoveTalkPage) != 0 && fromTitle.Namespace.IsTalkSpace;
				var moveSubPages = (this.MoveExtra & MoveOptions.MoveSubPages) != 0 && fromTitle.Namespace.AllowsSubpages;
				this.Site.Move(
					fromTitle,
					replacement.To,
					this.EditSummaryMove,
					moveTalkPage,
					moveSubPages,
					this.RedirectOption == RedirectOption.Suppress);
				if (this.MoveDelay > 0)
				{
					// Quick hack of a delay, since UESP sometimes seems to lag, but may be version-specific, so don't want to make it too formal of a thing.
					Thread.Sleep(this.MoveDelay);
				}

				this.Progress++;
			}
		}

		protected virtual void ReplaceBacklinks(Page page, NodeCollection nodes)
		{
			page.ThrowNull();

			// Possibly better as a visitor class?
			this.isFirstLink = true;
			foreach (var node in nodes.NotNull())
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

		protected virtual void GetPageActions(PageCollection fromPages)
		{
			var toPages = this.GetToPages();
			this.replacements.Sort();
			foreach (var replacement in this.replacements)
			{
				if (!replacement.MoveActions.HasAction(ReplacementActions.Skip))
				{
					var fromPage = fromPages[replacement.From];
					var actions = this.GetPageActions(replacement, fromPage, toPages);
					replacement.SetMoveActions(actions);
				}
			}
		}

		protected virtual void SetupProposedDeletions(PageCollection pageInfo, IReadOnlyDictionary<Title, TitleCollection> catMembers)
		{
			if ((this.FollowUpActions & FollowUpActions.ProposeUnused) == 0)
			{
				return;
			}

			var deletions = this.LoadProposedDeletions();
			TitleCollection doNotDelete = new(this.Site);
			foreach (var template in this.Site.DeletePreventionTemplates)
			{
				doNotDelete.GetBacklinks(template.FullPageName, BacklinksTypes.EmbeddedIn, true);
			}

			foreach (var replacement in this.replacements)
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
							replacement.SetMoveActionFlag(ReplacementActions.Move, "no links, but marked to not be deleted");
						}
					}
					else if (deletions.Contains(fromPage))
					{
						replacement.SetMoveActions(ReplacementActions.Skip, "already proposed for deletion");
					}
					else
					{
						replacement.SetMoveActionFlag(ReplacementActions.Propose, "appears to be unused");
					}
				}
			}
		}

		protected virtual void UpdateGalleryLinks(Page page, ITagNode tag)
		{
			var text = tag.NotNull().InnerText;
			if (text == null)
			{
				return;
			}

			StringBuilder sb = new();
			var lines = text.Split(TextArrays.LineFeed);
			foreach (var line in lines)
			{
				var newLine = line;
				if (line.Length > 0)
				{
					try
					{
						SiteLink link = SiteLink.FromGalleryText(this.Site, line);
						if (this.replacements.TryGetValue(link, out var replacement)
							&& replacement.MoveActions.HasAction(ReplacementActions.UpdateLinks))
						{
							if (replacement.To.Namespace != MediaWikiNamespaces.File)
							{
								this.Warn($"{replacement.From.PageName} to non-File {replacement.From.FullPageName} move skipped in gallery on page: {page.FullPageName}.");
								continue;
							}

							var newPageName = replacement.To.PageName;
							var newNamespace = (replacement.From.Namespace == replacement.To.Namespace && link.Coerced) ? this.Site[MediaWikiNamespaces.Main] : replacement.To.Namespace;
							var newTitle = CreateTitle.FromValidated(newNamespace, newPageName);
							var newLink = link.With(newTitle);
							this.UpdateLinkText(page, replacement.From, newLink, false);
							newLine = newLink.ToString()[2..^2].TrimEnd();
						}
					}
					catch (ArgumentException)
					{
						this.StatusWriteLine($"Malformed gallery link on {page.FullPageName} in line {line}");
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
			page.ThrowNull();
			SiteLink link = SiteLink.FromLinkNode(this.Site, node.NotNull());
			if (this.replacements.TryGetValue(link, out var replacement)
				&& replacement.MoveActions.HasAction(ReplacementActions.UpdateLinks)
				&& (link.ForcedNamespaceLink
					|| link.Namespace != MediaWikiNamespaces.Category
					|| replacement.To.Namespace != MediaWikiNamespaces.Category
					|| (this.FollowUpActions & FollowUpActions.UpdateCategoryMembers) != 0))
			{
				link = link.With(replacement.To);
				this.UpdateLinkText(page, replacement.From, link, !isRedirectTarget);
				link.UpdateLinkNode(node);
			}

			if (link.Namespace == MediaWikiNamespaces.Media)
			{
				var key = CreateTitle.FromValidated(this.Site, MediaWikiNamespaces.File, link.PageName);
				if (this.replacements.TryGetValue(link.With(key), out replacement) &&
					replacement.MoveActions.HasAction(ReplacementActions.UpdateLinks))
				{
					var newtitle = CreateTitle.FromValidated(this.Site, MediaWikiNamespaces.Media, replacement.To.PageName);
					link = link.With(newtitle);
					this.UpdateLinkText(page, replacement.From, link, !isRedirectTarget);
					link.UpdateLinkNode(node);
				}
			}
		}

		protected virtual void UpdateLinkText(Page page, Title oldTitle, SiteLink newLink, bool addCaption)
		{
			page.ThrowNull();
			oldTitle.ThrowNull();
			newLink.ThrowNull();
			if ((this.FollowUpActions & FollowUpActions.UpdateCaption) != 0)
			{
				// If UpdateCaption is true and caption exactly matches either the full page name or the simple page name, update it.
				if (newLink.Text != null
					&& page.Namespace != MediaWikiNamespaces.User
					&& !page.Site.IsDiscussionPage(page))
				{
					TitleFactory textTitle = TitleFactory.Create(this.Site, MediaWikiNamespaces.Main, newLink.Text);
					if (oldTitle is IFullTitle fullTitle && fullTitle.FullEquals(textTitle))
					{
						newLink.Text = textTitle.ToString();
					}
					else if (oldTitle.Equals(textTitle))
					{
						var simp = this.replacements[oldTitle].To;
						newLink.Text = simp.ToString();
					}
					else if ((this.FollowUpActions & FollowUpActions.UpdatePageNameCaption) != 0 && string.Equals(oldTitle.PageName, newLink.Text, StringComparison.Ordinal))
					{
						var simp = this.replacements[oldTitle].To;
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
			page.ThrowNull();
			if (this.replacements.TryGetValue(template.NotNull().TitleValue, out var replacement) &&
				replacement.MoveActions.HasAction(ReplacementActions.UpdateLinks))
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
		private void AddCategoryMembers(IReadOnlyDictionary<Title, TitleCollection> categoryMembers, TitleCollection backlinkTitles)
		{
			foreach (var replacement in this.replacements)
			{
				if (replacement.MoveActions.HasAction(ReplacementActions.UpdateLinks) &&
					replacement.From.Namespace == replacement.To.Namespace &&
					categoryMembers.TryGetValue(replacement.From, out var catMembers))
				{
					backlinkTitles.AddRange(catMembers);
				}
			}
		}

		private void FullEdit(object sender, Page page)
		{
			ContextualParser parser = new(page);
			if (this.replacements.TryGetValue(page, out var replacement) &&
				replacement.MoveActions.HasAction(ReplacementActions.NeedsEdited))
			{
				this.EditPageLoaded(parser, replacement);
			}

			this.BacklinkPageLoaded(parser);
			parser.UpdatePage();
		}

		private TitleCollection GetBacklinkTitles(PageCollection pageInfo)
		{
			TitleCollection retval = new(this.Site);
			foreach (var replacement in this.replacements)
			{
				if (pageInfo[replacement.From] is Page fromPage &&
					replacement.MoveActions.HasAction(ReplacementActions.UpdateLinks))
				{
					foreach (var backlink in fromPage.Backlinks)
					{
						retval.Add(backlink.Key);
					}
				}
			}

			return retval;
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
			foreach (var replacement in this.replacements)
			{
				fromTitles.Add(replacement.From);
			}

			PageCollection retval = PageCollection.Unlimited(this.Site, modules, false);
			retval.GetTitles(fromTitles);

			return retval;
		}

		private TitleCollection GetLoadTitles(PageCollection fromPages, IReadOnlyDictionary<Title, TitleCollection> categoryMembers)
		{
			TitleCollection loadTitles = new(this.Site);
			if ((this.FollowUpActions & FollowUpActions.AffectsBacklinks) != 0)
			{
				var backlinkTitles = this.GetBacklinkTitles(fromPages);
				if ((this.FollowUpActions & FollowUpActions.UpdateCategoryMembers) != 0)
				{
					this.AddCategoryMembers(categoryMembers, backlinkTitles);
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

			return loadTitles;
		}

		private Replacement.DetailedActions GetPageActions(Replacement replacement, Page fromPage, PageCollection toPages)
		{
			var actions = replacement.MoveActions.NotNull(nameof(replacement), nameof(replacement.MoveActions)).Actions;
			if (this.MoveAction == MoveAction.None && !replacement.From.SimpleEquals(replacement.To))
			{
				return new(actions | ReplacementActions.UpdateLinks, replacement.MoveActions.Reason);
			}

			// Any proposed move.
			if (!fromPage.Exists)
			{
				// From title does not exist.
				return new(ReplacementActions.Skip, "page doesn't exist");
			}

			if ((actions & ReplacementActions.Move) != 0 && toPages.Contains(replacement.To))
			{
				return (actions & ReplacementActions.Propose) != 0
					? this.HandleConflict(replacement)
					: new(ReplacementActions.Skip, "To page exists");
			}

			if (!replacement.From.SimpleEquals(replacement.To))
			{
				actions |= ReplacementActions.Move | ReplacementActions.UpdateLinks;
				if (this.RedirectOption == RedirectOption.CreateButProposeDeletion && !fromPage.IsRedirect)
				{
					return new(actions | ReplacementActions.Propose, "redirect from page move");
				}
			}

			return new Replacement.DetailedActions(actions, string.Empty);
		}

		private PageCollection GetToPages()
		{
			PageCollection toPages = PageCollection.Unlimited(this.Site, PageModules.Info, false);
			if (this.MoveAction == MoveAction.MoveSafely)
			{
				TitleCollection toTitles = new(this.Site);
				foreach (var replacement in this.replacements)
				{
					if (replacement.MoveActions.HasAction(ReplacementActions.Move))
					{
						toTitles.Add(replacement.To);
					}
				}

				toPages.GetTitles(toTitles);
				toPages.RemoveExists(false);
			}

			return toPages;
		}

		private void ReadJsonFile()
		{
			var repFile = File.ReadAllText(this.replacementStatusFile);
			var reps = JsonConvert.DeserializeObject<IEnumerable<Replacement>>(repFile, this.titleConverter) ?? throw new InvalidOperationException();
			this.replacements.AddRange(reps);
		}

		private void ValidateActions()
		{
			foreach (var replacement in this.replacements)
			{
				if (replacement.MoveActions.Actions == ReplacementActions.None)
				{
					this.Warn($"Replacement Action for {replacement.From} is unknown.");
				}
			}
		}

		private void ValidateMoves()
		{
			var inPlaceMoves = false;
			Dictionary<Title, Replacement> unique = new();
			foreach (var replacement in this.replacements)
			{
				if (replacement.MoveActions.HasAction(ReplacementActions.Move) &&
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
					existing.SetMoveActions(ReplacementActions.Skip, "duplicate To page");
					replacement.SetMoveActions(ReplacementActions.Skip, "duplicate To page");
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
			var newFile = JsonConvert.SerializeObject(this.replacements, Formatting.Indented, this.titleConverter);
			File.WriteAllText(this.replacementStatusFile, newFile);
		}
		#endregion
	}
}
