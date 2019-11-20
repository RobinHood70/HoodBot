﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
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
		FixTalkLinks = 1 << 3,
		ProposeUnused = 1 << 4,
		ReplaceSameNamedLinks = 1 << 5,
		Default = CheckLinksRemaining | EmitReport | FixLinks | FixTalkLinks,
	}

	public enum MoveAction
	{
		Move,
		RenameOnly,
		Skip
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
		#region Static Fields
		private static readonly Regex GalleryFinder = new Regex(@"(?<open><gallery(\ [^>]*?)?>\s*\n)(?<content>.*?)(?<close>\s*\n</gallery>)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		#endregion

		#region Fields
		private readonly Regex alreadyProposed;
		private readonly Dictionary<Title, Title> movedPages = new Dictionary<Title, Title>();
		private readonly Regex doNotDelete;
		private string? logDetails = null;
		#endregion

		#region Constructors
		protected PageMoverJob(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.BacklinkTitles = new TitleCollection(site);
			this.EditPages = new TitleCollection(site);
			this.alreadyProposed = Template.Find(this.Site.DeletionTemplates.ToPageNames());
			this.doNotDelete = Template.Find(this.Site.DeletePreventionTemplates.ToPageNames());
		}
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
					else if (this.MoveAction == MoveAction.Skip)
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
		protected static string BacklinksFile => Environment.ExpandEnvironmentVariables(@"%BotData%\Backlinks.txt");

		protected static string ReplacementStatusFile => Environment.ExpandEnvironmentVariables(@"%BotData%\Replacements.json");
		#endregion

		#region Protected Properties
		protected TitleCollection BacklinkTitles { get; }

		protected bool DoReport { get; set; } = true;

		protected Action<EditJob, Page>? EditPageMethod { get; } = null;

		protected TitleCollection EditPages { get; }

		protected string EditSummaryEditAfterMove { get; set; } = "Update text after page move";

		protected string EditSummaryMove { get; set; } = "Rename";

		protected string EditSummaryUpdateLinks { get; set; } = "Update links after page move";

		protected FollowUpActions FollowUpActions { get; set; } = FollowUpActions.Default;

		protected MoveAction MoveAction { get; set; } = MoveAction.Move;

		protected MoveOptions MoveOptions { get; set; } = MoveOptions.None;

		protected bool MoveOverExisting { get; set; } = false;

		protected RedirectOption RedirectOption { get; set; } = RedirectOption.Suppress;

		protected IList<Replacement> Replacements { get; } = new List<Replacement>();

		protected bool SuppressRedirects { get; set; } = true;
		#endregion

		#region Protected Methods

		protected void AddReplacement(string from, string to) => this.Replacements.Add(new Replacement(this.Site, from, to));

		protected void AddReplacement(Title from, Title to) => this.Replacements.Add(new Replacement(from, to));

		// [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Optiona, to be called only when necessary.")]
		protected void LoadReplacementsFromFile(string fileName)
		{
			var repFile = File.ReadLines(fileName);
			foreach (var line in repFile)
			{
				var rep = line.Split('\t');
				this.AddReplacement(rep[0].Trim(), rep[1].Trim());
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			if (this.MoveAction != MoveAction.Skip || this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused))
			{
				this.MovePages();
			}

			if (this.EditPages.Count > 0)
			{
				this.EditAfterMove();
			}

			if (this.FollowUpActions.HasFlag(FollowUpActions.FixLinks) && this.BacklinkTitles.Count > 0)
			{
				this.FixLinks();
			}

			if (this.FollowUpActions.HasFlag(FollowUpActions.CheckLinksRemaining))
			{
				this.CheckRemaining();
			}

			File.Delete(BacklinksFile);
			File.Delete(ReplacementStatusFile);
		}

		protected override void BeforeLogging()
		{
			var titleConverter = new TitleJsonConverter(this.Site);
			this.StatusWriteLine("Getting Replacement List");
			if (File.Exists(ReplacementStatusFile) && File.Exists(BacklinksFile))
			{
				// TODO: Rewrite to manually create and populate replacements so class can respect properties that should be read-only.
				var repFile = File.ReadAllText(ReplacementStatusFile);
				if (JsonConvert.DeserializeObject<ICollection<Replacement>>(repFile, titleConverter) is ICollection<Replacement> replacements)
				{
					this.Replacements.Clear();
					this.Replacements.AddRange(replacements);
				}

				var backlinkLines = File.ReadAllLines(BacklinksFile, Encoding.Unicode);
				this.BacklinkTitles.Add(backlinkLines);
			}
			else
			{
				this.PopulateReplacements();
				var backlinks = this.SetupAndGetBacklinks();
				this.BacklinkTitles.AddRange(backlinks);
				var newFile = JsonConvert.SerializeObject(this.Replacements, Formatting.Indented, titleConverter);
				File.WriteAllText(ReplacementStatusFile, newFile);
				File.WriteAllLines(BacklinksFile, this.BacklinkTitles.ToFullPageNames(), Encoding.Unicode);
			}

			if (this.FollowUpActions.HasFlag(FollowUpActions.EmitReport))
			{
				// Normally a duplication of effort, but if an interruption occurred after figuring out what to do, but before saving the report, then we have no report, so do it possibly-again. Wiki will ignore it if it's identical.
				this.EmitReport();
			}
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract void PopulateReplacements();
		#endregion

		#region Protected Virtual Methods
		protected virtual void CheckRemaining()
		{
			this.StatusWriteLine("Checking remaining pages");
			this.ProgressMaximum = this.movedPages.Count;
			var leftovers = new TitleCollection(this.Site);
			foreach (var page in this.movedPages)
			{
				var backlinks = new TitleCollection(this.Site);
				backlinks.GetBacklinks(page.Key.FullPageName, BacklinksTypes.All, true);
				backlinks.Remove(page.Key);
				this.FilterSitePages(backlinks);
				if (backlinks.Count > 0)
				{
					leftovers.Add(page.Key);
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

		protected virtual void CustomReplaceGeneral(Page page)
		{
		}

		protected virtual void CustomReplaceSpecific(Page page, Title from, Title to)
		{
		}

		protected virtual void EditAfterMove()
		{
			var pages = PageCollection.Unlimited(this.Site);
			pages.GetTitles(this.EditPages);
			foreach (var replacement in this.Replacements)
			{
				if (replacement.DeleteReason != null)
				{
					var deleteTemplate = new Template("Proposeddeletion")
						{
							{ "bot", "1" }
						};
					deleteTemplate.AddAnonymous(replacement.DeleteReason);
					this.ProposeForDeletion(pages[replacement.From.FullPageName], deleteTemplate);
				}

				if (replacement.Action != ReplacementAction.Skip && this.EditPageMethod != null)
				{
					var title = this.Site.EditingEnabled ? replacement.To : replacement.From;
					var page = pages[title.FullPageName];
					this.EditConflictAction = this.EditPageMethod;
					this.SavePage(page, this.EditSummaryEditAfterMove, true);
				}
			}
		}

		protected virtual void EmitReport()
		{
			this.WriteLine("{| class=\"wikitable sortable\"");
			this.WriteLine("! Page !! Action");
			foreach (var rep in this.Replacements)
			{
				this.WriteLine("|-");
				this.Write(Invariant($"| {rep.From} ([[Special:WhatLinksHere/{rep.From}|links]]) || "));

				switch (rep.Action)
				{
					case ReplacementAction.Unknown:
						this.Write(this.FollowUpActions.HasFlag(FollowUpActions.FixLinks) ? "Fix links only" : "Unknown! This should never happen.");
						break;
					case ReplacementAction.Move:
						this.Write($"Move to {SiteLink.LinkTextFromTitle(rep.To)}");
						break;
					case ReplacementAction.Skip:
						this.Write("Skip");
						break;
					case ReplacementAction.ProposeForDeletion:
						this.Write("Propose for deletion");
						break;
				}

				if (rep.ActionReason != null)
				{
					this.Write(" (" + rep.ActionReason + ")");
				}

				this.WriteLine();
			}

			this.WriteLine("|}");
		}

		protected virtual void FilterBacklinks(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			backlinkTitles.RemoveNamespaces(
				MediaWikiNamespaces.Main,
				MediaWikiNamespaces.Media,
				MediaWikiNamespaces.MediaWiki,
				MediaWikiNamespaces.Project,
				MediaWikiNamespaces.Special,
				MediaWikiNamespaces.Template);
		}

		protected virtual void FilterSitePages(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			foreach (var title in this.Site.FilterPages)
			{
				backlinkTitles.Remove(title);
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
			this.StatusWriteLine("Replacing links");
			this.ProgressMaximum = this.BacklinkTitles.Count + 1;
			var backlinks = PageCollection.Unlimited(this.Site);
			backlinks.PageLoaded += this.BacklinkLoaded;
			backlinks.GetTitles(this.BacklinkTitles);
			backlinks.PageLoaded -= this.BacklinkLoaded;
			backlinks.Sort();

			this.EditConflictAction = this.BacklinkLoaded;
			foreach (var page in backlinks)
			{
				this.SavePage(page, this.EditSummaryUpdateLinks, true);
			}
		}

		protected virtual void MovePages()
		{
			this.StatusWriteLine("Moving pages and proposing deletions");
			this.ProgressMaximum = this.Replacements.Count;
			this.movedPages.Clear();
			foreach (var replacement in this.Replacements)
			{
				if (replacement.Action == ReplacementAction.Move)
				{
					var result = replacement.From.Move(
						replacement.To.FullPageName,
						this.EditSummaryMove,
						this.MoveOptions.HasFlag(MoveOptions.MoveTalkPage),
						this.MoveOptions.HasFlag(MoveOptions.MoveSubPages),
						this.RedirectOption == RedirectOption.Suppress);
					if (result.Value is IDictionary<string, string> values)
					{
						foreach (var entry in values)
						{
							var from = new Title(this.Site, entry.Key);
							var to = new Title(this.Site, entry.Value);
							this.movedPages.Add(from, to);
						}
					}
				}

				this.Progress++;
			}
		}

		protected virtual TitleCollection SetupAndGetBacklinks()
		{
			this.StatusWriteLine("Figuring out what to do");
			this.ProgressMaximum = this.Replacements.Count + 1;
			this.Progress++;

			var (fromPages, toPages) = this.GetReplacementPages();
			var backlinkTitles = new TitleCollection(this.Site);
			foreach (var replacement in this.Replacements)
			{
				if (replacement.Action == ReplacementAction.Unknown && (this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused) || this.FollowUpActions.HasFlag(FollowUpActions.FixLinks)))
				{
					var tempTitles = new TitleCollection(this.Site);
					tempTitles.GetBacklinks(replacement.From.FullPageName, BacklinksTypes.All);
					if (replacement.From.NamespaceId == MediaWikiNamespaces.Category)
					{
						tempTitles.GetCategoryMembers(replacement.From.FullPageName);
					}

					backlinkTitles.AddRange(tempTitles);
					if (this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused) && tempTitles.Count > 0)
					{
						var fromPage = fromPages.ValueOrDefault(replacement.From.FullPageName);
						if (fromPage == null || !fromPage.Exists)
						{
							throw new ArgumentNullException($"Page doesn't exist: {replacement.From}");
						}

						if (this.doNotDelete.IsMatch(fromPage.Text))
						{
							if (this.MoveAction != MoveAction.Skip)
							{
								replacement.Action = ReplacementAction.Move;
								replacement.ActionReason = "No links, but {{tl|Linked image}} present - move instead of proposing for deletion";
							}
						}
						else if (this.alreadyProposed.IsMatch(fromPage.Text))
						{
							replacement.Action = ReplacementAction.Skip;
							replacement.ActionReason = "Already proposed for deletion";
						}
						else
						{
							replacement.Action = ReplacementAction.ProposeForDeletion;
							replacement.ActionReason = "Unused";
							replacement.DeleteReason = "Unused";
							this.EditPages.Add(replacement.From);
						}
					}
				}

				if (replacement.Action == ReplacementAction.Unknown && this.MoveAction != MoveAction.Skip)
				{
					if (!this.MoveOverExisting && toPages.Contains(replacement.To))
					{
						replacement.Action = ReplacementAction.Skip;
						replacement.ActionReason = string.Format($"{SiteLink.LinkTextFromTitle(replacement.To)} exists");
					}
					else
					{
						replacement.Action = ReplacementAction.Move;
						replacement.ActionReason = "Standard move";
						if (this.RedirectOption != RedirectOption.Suppress)
						{
							replacement.ActionReason += ", propose redirect for deletion after move";
							replacement.DeleteReason = "Redirect from page move";
							this.EditPages.Add(replacement.From);
						}
					}
				}

				if (replacement.Action == ReplacementAction.Move && this.EditPageMethod != null)
				{
					this.EditPages.Add(this.Site.EditingEnabled ? replacement.To : replacement.From);
				}

				this.Progress++;
			}

			foreach (var replacement in this.Replacements)
			{
				if (backlinkTitles.Contains(replacement.From))
				{
					// By the time we access backlinkTitles, the pages should already have been moved, so use the To title if there's overlap.
					backlinkTitles.Add(replacement.To);
					backlinkTitles.Remove(replacement.From);
				}
			}

			this.FilterBacklinks(backlinkTitles);
			this.FilterSitePages(backlinkTitles);

			return backlinkTitles;
		}
		#endregion

		#region Private Static Methods
		private static string ReplaceGalleryLinks(Match match, Title fromTitle, Title toTitle)
		{
			var sb = new StringBuilder();
			var replacementsMade = false;
			var lines = match.Groups["content"].Value.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
			foreach (var line in lines)
			{
				var pageName = line.Split(TextArrays.Pipe, 2)[0];
				var title = new TitleParts(fromTitle.Site, pageName);
				if (fromTitle.PageName == title.PageName)
				{
					if (fromTitle.NamespaceId == MediaWikiNamespaces.File)
					{
						sb.Append(fromTitle.Namespace.DecoratedName);
					}

					sb.Append(toTitle.PageName);
					if (pageName.Length == 2)
					{
						sb.Append(pageName[1]);
					}

					sb.Append('\n');

					replacementsMade = true;
				}
			}

			return replacementsMade ? match.Groups["open"].Value.Trim() + "\n" + sb.ToString() + match.Groups["close"].Value.Trim() : match.Value;
		}

		private void ReplaceLinksAndTemplates(Page currentPage)
		{
			var text = currentPage.Text;

			// Page may not have been correctly found if it was recently moved. If it wasn't, there's little we can do here, so skip it and it'll show up in the report (assuming it's generated).
			// TODO: See if this can be worked around, like asking the wiki to purge and reload or something.
			if (text != null)
			{
				var parser = WikiTextParser.Parse(text);
				new BacklinkReplaceVisitor(this.Site, this.movedPages).Build(parser);
				text = WikiTextVisitor.Raw(parser);

				// Galleries - handled here for now, but might be able to move it into ReplaceVisitor if parsed into a TagNode or custom GalleryNode.
				foreach (var replacement in this.Replacements)
				{
					if (replacement.From.NamespaceId == MediaWikiNamespaces.File)
					{
						text = GalleryFinder.Replace(text, (match) => ReplaceGalleryLinks(match, replacement.From, replacement.To));
					}
				}

				currentPage.Text = text;
			}
		}
		#endregion

		#region Private Methods
		private void BacklinkLoaded(object sender, Page page)
		{
			this.ReplaceLinksAndTemplates(page);
			foreach (var movedPage in this.movedPages)
			{
				this.CustomReplaceSpecific(page, movedPage.Key, movedPage.Value);
			}

			this.CustomReplaceGeneral(page);
		}

		private (PageCollection FromTitles, PageCollection ToTitles) GetReplacementPages()
		{
			var fromTitles = new TitleCollection(this.Site);
			var toTitles = new TitleCollection(this.Site);
			foreach (var replacement in this.Replacements)
			{
				fromTitles.Add(replacement.From);
				toTitles.Add(replacement.To);
			}

			var fromPages = PageCollection.Unlimited(this.Site);
			if (this.FollowUpActions.HasFlag(FollowUpActions.ProposeUnused))
			{
				fromPages.GetTitles(fromTitles);
			}

			var toPages = PageCollection.Unlimited(this.Site, new PageLoadOptions(PageModules.None)); // Only worried about existence, so don't load anything other than that.
			if (!this.MoveOverExisting)
			{
				toPages.GetTitles(toTitles);
				toPages.RemoveNonExistent();
			}

			return (fromPages, toPages);
		}

		private void ProposeForDeletion(Page page, Template deletionTemplate)
		{
			ThrowNull(page, nameof(page));
			ThrowNull(deletionTemplate, nameof(deletionTemplate));
			var deletionText = deletionTemplate.ToString();
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
	}
}