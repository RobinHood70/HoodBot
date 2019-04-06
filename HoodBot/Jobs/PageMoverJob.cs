namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;
	using Newtonsoft.Json;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	#region Public Enums
	[Flags]
	public enum TaskActions
	{
		Move = 1,
		MoveTalkPage = 1 << 1,
		MoveSubpages = 1 << 2,
		CheckLinksRemaining = 1 << 3,
		CheckToExists = 1 << 4,
		EditAfterMove = 1 << 5,
		FixLinks = 1 << 6,
		ProposeRedirects = 1 << 7,
		ProposeUnused = 1 << 8,
		SuppressRedirect = 1 << 9,
		SaveResults = 1 << 10,
		Default = CheckLinksRemaining | CheckToExists | FixLinks | Move | MoveTalkPage | SuppressRedirect | SaveResults,
	}
	#endregion

	public abstract class PageMoverJob : EditJob
	{
		#region Static Fields
		private static readonly Regex GalleryFinder = new Regex(@"(?<open><gallery(\ [^>]*?)?>\s*\n)(?<content>.*?)(?<close>\s*\n</gallery>)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		#endregion

		#region Fields
		private readonly TitleJsonConverter titleConverter;
		private readonly TitleCollection editPages;
		#endregion

		#region Constructors
		protected PageMoverJob(Site site, AsyncInfo asyncInfo, TaskActions taskActions)
			: base(site, asyncInfo)
		{
			this.TaskActions = taskActions;
			this.TalkLikePages = new TitleCollection(site);
			this.BacklinkTitles = new TitleCollection(site);
			this.titleConverter = new TitleJsonConverter(this.Site);
			this.editPages = new TitleCollection(site);

			var actions = new List<string>();
			if (taskActions.HasFlag(TaskActions.Move))
			{
				actions.Add(
					taskActions.HasFlag(TaskActions.SuppressRedirect) ? "suppress redirects" :
					taskActions.HasFlag(TaskActions.ProposeRedirects) ? "create redirects but propose for deletion" :
					"create redirects");
				if (taskActions.HasFlag(TaskActions.CheckToExists))
				{
					actions.Add("check destination empty before move");
				}
			}
			else
			{
				actions.Add("do not move pages");
			}

			if (taskActions.HasFlag(TaskActions.ProposeUnused))
			{
				actions.Add("propose unused pages for deletion");
			}

			if (taskActions.HasFlag(TaskActions.CheckLinksRemaining))
			{
				actions.Add("report any remaining links");
			}

			if (taskActions.HasFlag(TaskActions.SaveResults))
			{
				site.UserFunctions.InitializeResult(ResultDestination.ResultsPage, null, "Results of " + this.LogName);
			}

			this.LogDetails = string.Join(", ", actions).UpperFirst(CultureInfo.CurrentCulture);
		}
		#endregion

		#region Public Override Properties
		public override string LogName
		{
			get
			{
				if (this.TaskActions.HasFlag(TaskActions.FixLinks) && !this.TaskActions.HasFlag(TaskActions.Move))
				{
					return "Link Replacer";
				}

				var retval = "Page Mover";
				if (this.TaskActions.HasFlag(TaskActions.ProposeRedirects) || this.TaskActions.HasFlag(TaskActions.ProposeUnused))
				{
					return retval + " (Propose Deletion Mode)";
				}

				if (this.TaskActions.HasFlag(TaskActions.SaveResults))
				{
					return retval + " (Report Only)";
				}

				if (!this.TaskActions.HasFlag(TaskActions.Move))
				{
					retval += " (Unknown Configuration)";
				}

				return retval;
			}
		}
		#endregion

		#region Protected Static Properties
		protected static string BacklinksFile => Environment.ExpandEnvironmentVariables(@"%BotData%\Backlinks.txt");

		protected static string ReplacementStatusFile => Environment.ExpandEnvironmentVariables(@"%BotData%\Replacements.json");
		#endregion

		#region Protected Properties
		protected TitleCollection BacklinkTitles { get; }

		protected bool DoReport { get; set; } = true;

		protected string EditSummaryEditAfterMove { get; set; } = "Update text after page move";

		protected string EditSummaryMove { get; set; } = "Move page";

		protected string EditSummaryUpdateLinks { get; set; } = "Update links after page move";

		protected ICollection<Replacement> Replacements { get; private set; }

		protected TitleCollection TalkLikePages { get; }

		protected TaskActions TaskActions { get; set; } = TaskActions.Default;
		#endregion

		#region Public Static Methods
		public static IList<Replacement> LoadReplacementsFromFile(Site site, string fileName)
		{
			var retval = new List<Replacement>();
			var repFile = File.ReadLines(fileName);
			foreach (var line in repFile)
			{
				var rep = line.Split('\t');
				retval.Add(new Replacement(site, rep[0].Trim(), rep[1].Trim()));
			}

			return retval;
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			if (this.TaskActions.HasFlag(TaskActions.Move) || this.TaskActions.HasFlag(TaskActions.ProposeUnused))
			{
				this.DoMoves();
			}

			if (this.editPages.Count > 0)
			{
				this.PostMoveEdits();
			}

			if (this.TaskActions.HasFlag(TaskActions.FixLinks) && this.BacklinkTitles.Count > 0)
			{
				this.DoFixLinks();
			}

			if (this.Site.EditingEnabled && this.TaskActions.HasFlag(TaskActions.CheckLinksRemaining))
			{
				this.DoPostCheck();
			}

			File.Delete(BacklinksFile);
			File.Delete(ReplacementStatusFile);
		}

		protected override void PrepareJob()
		{
			this.TalkLikePages.GetCategoryMembers("Message Boards", false);
			this.StatusWriteLine("Getting Replacement List");
			if (File.Exists(ReplacementStatusFile) && File.Exists(BacklinksFile))
			{
				var repFile = File.ReadAllText(ReplacementStatusFile);
				this.Replacements = JsonConvert.DeserializeObject<ICollection<Replacement>>(repFile, this.titleConverter);
				var backlinkLines = File.ReadAllLines(BacklinksFile, Encoding.Unicode);
				this.BacklinkTitles.Add(backlinkLines);
			}
			else
			{
				this.Replacements = this.GetReplacements();
				var backlinks = this.SetupAndGetBacklinks();
				this.BacklinkTitles.AddRange(backlinks);
				var newFile = JsonConvert.SerializeObject(this.Replacements, Formatting.Indented, this.titleConverter);
				File.WriteAllText(ReplacementStatusFile, newFile);
				File.WriteAllLines(BacklinksFile, this.BacklinkTitles.ToFullPageNames(), Encoding.Unicode);
			}

			if (this.TaskActions.HasFlag(TaskActions.SaveResults))
			{
				// Normally a duplication of effort, but if an interruption occurred after figuring out what to do, but before saving the report, then we have no report, so do it possibly-again. Wiki will ignore it if it's identical.
				this.EmitReport();
			}
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract ICollection<Replacement> GetReplacements();
		#endregion

		#region Protected Virtual Methods
		protected virtual void CustomReplace(Page page)
		{
		}

		protected virtual void CustomReplace(Page page, Replacement replacement)
		{
		}

		protected virtual void DoFixLinks()
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

		protected virtual void DoMoves()
		{
			this.StatusWriteLine("Moving pages and proposing deletions");
			this.ProgressMaximum = this.Replacements.Count;
			foreach (var replacement in this.Replacements)
			{
				if (replacement.Action == ReplacementAction.Move)
				{
					replacement.From.Move(replacement.To.FullPageName, this.EditSummaryMove, this.TaskActions.HasFlag(TaskActions.MoveTalkPage), this.TaskActions.HasFlag(TaskActions.MoveSubpages), this.TaskActions.HasFlag(TaskActions.SuppressRedirect));
				}

				this.Progress++;
			}
		}

		protected virtual void PostMoveEdits()
		{
			var pages = PageCollection.Unlimited(this.Site);
			pages.GetTitles(this.editPages);
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

				if (replacement.Action == ReplacementAction.Move && this.TaskActions.HasFlag(TaskActions.EditAfterMove))
				{
					var title = this.Site.EditingEnabled ? replacement.To : replacement.From;
					var page = pages[title.FullPageName];
					this.EditConflictAction = this.EditAfterMove;
					this.SavePage(page, this.EditSummaryEditAfterMove, true);
				}
			}
		}

		protected virtual void DoPostCheck()
		{
			this.StatusWriteLine("Checking remaining pages");
			this.ProgressMaximum = this.Replacements.Count;
			var leftovers = new List<Title>();
			foreach (var replacement in this.Replacements)
			{
				if (replacement.Action == ReplacementAction.Move)
				{
					var backlinks = new TitleCollection(this.Site);
					backlinks.GetBacklinks(replacement.To.FullPageName, BacklinksTypes.All, true);
					backlinks.Remove(replacement.To);
					this.FilterSitePages(backlinks);
					if (backlinks.Count > 0)
					{
						leftovers.Add(replacement.To);
					}

					this.Progress++;
				}
			}

			if (leftovers.Count > 0)
			{
				this.WriteLine("The following pages are still linked to:");
				foreach (var title in leftovers)
				{
					this.WriteLine($"* [[Special:WhatLinksHere/{title}|{title}]]");
				}
			}
		}

		protected virtual void EditAfterMove(EditJob sender, Page page) => throw new InvalidOperationException("EditAfterMove requested, but no custom method specified.");

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
						this.Write(this.TaskActions.HasFlag(TaskActions.FixLinks) ? "Fix links only" : "Unknown! This should never happen.");
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

		protected virtual TitleCollection SetupAndGetBacklinks()
		{
			this.StatusWriteLine("Figuring out what to do");
			this.ProgressMaximum = this.Replacements.Count + 1;
			this.Progress++;

			var fromTitles = new TitleCollection(this.Site);
			var toTitles = new TitleCollection(this.Site);
			foreach (var replacement in this.Replacements)
			{
				fromTitles.Add(replacement.From);
				toTitles.Add(replacement.To);
			}

			PageCollection fromPages = null;
			if (this.TaskActions.HasFlag(TaskActions.ProposeUnused))
			{
				fromPages = fromTitles.Load(); // Need content to check proposed deletion status.
			}

			PageCollection toPages = null;
			if (this.TaskActions.HasFlag(TaskActions.CheckToExists))
			{
				toPages = toTitles.Load(PageModules.None); // Only worried about existence, so don't load anything other than that.
				toPages.RemoveNonExistent();
			}

			var backlinkTitles = new TitleCollection(this.Site);
			foreach (var replacement in this.Replacements)
			{
				if (replacement.Action == ReplacementAction.Unknown && (this.TaskActions.HasFlag(TaskActions.ProposeUnused) || this.TaskActions.HasFlag(TaskActions.FixLinks)))
				{
					var tempTitles = new TitleCollection(this.Site);
					tempTitles.GetBacklinks(replacement.From.FullPageName, BacklinksTypes.All);
					if (replacement.From.Namespace == MediaWikiNamespaces.Category)
					{
						tempTitles.GetCategoryMembers(replacement.From.FullPageName, false);
					}

					backlinkTitles.AddRange(tempTitles);
					if (this.TaskActions.HasFlag(TaskActions.ProposeUnused) && tempTitles.Count > 0)
					{
						var prodResult = fromPages.TryGetValue(replacement.From.FullPageName, out var fromPage) ? this.CanDelete(fromPage) : ProposedDeletionResult.NonExistent;
						switch (prodResult)
						{
							case ProposedDeletionResult.AlreadyProposed:
								replacement.Action = ReplacementAction.Skip;
								replacement.ActionReason = "Already proposed for deletion";
								break;
							case ProposedDeletionResult.FoundNoDeleteRequest:
								if (this.TaskActions.HasFlag(TaskActions.Move))
								{
									replacement.Action = ReplacementAction.Move;
									replacement.ActionReason = "No links, but {{tl|Linked image}} present - move instead of proposing for deletion";
								}

								break;
							case ProposedDeletionResult.NonExistent:
								throw new ArgumentNullException($"Page doesn't exist: {replacement.From}");
							case ProposedDeletionResult.Add:
								replacement.Action = ReplacementAction.ProposeForDeletion;
								replacement.ActionReason = "Unused";
								replacement.DeleteReason = "Unused";
								this.editPages.Add(replacement.From);

								break;
						}
					}
				}

				if (replacement.Action == ReplacementAction.Unknown && this.TaskActions.HasFlag(TaskActions.Move))
				{
					if (this.TaskActions.HasFlag(TaskActions.CheckToExists) && toPages.Contains(replacement.To))
					{
						replacement.Action = ReplacementAction.Skip;
						replacement.ActionReason = string.Format($"{SiteLink.LinkTextFromTitle(replacement.To)} exists");
					}
					else
					{
						replacement.Action = ReplacementAction.Move;
						replacement.ActionReason = "Standard move";
						if (this.TaskActions.HasFlag(TaskActions.ProposeRedirects) && !this.TaskActions.HasFlag(TaskActions.SuppressRedirect))
						{
							replacement.ActionReason += ", propose redirect for deletion after move";
							replacement.DeleteReason = "Redirect from page move";
							this.editPages.Add(replacement.From);
						}
					}
				}

				if (replacement.Action == ReplacementAction.Move && this.TaskActions.HasFlag(TaskActions.EditAfterMove))
				{
					this.editPages.Add(this.Site.EditingEnabled ? replacement.To : replacement.From);
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
			var userFunctions = this.Site.UserFunctions;
			backlinkTitles.Remove(userFunctions.ResultsPage);
			backlinkTitles.Remove(userFunctions.LogPage);

			if (userFunctions is HoodBotFunctions hoodBotUserFunctions)
			{
				backlinkTitles.Remove(hoodBotUserFunctions.RequestsPage);
			}
		}

		protected virtual void FilterTalkLikePages(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			foreach (var catPage in this.TalkLikePages)
			{
				backlinkTitles.Remove(catPage);
			}
		}
		#endregion

		#region Private Static Methods
		private static string ReplaceGalleryLinks(Match match, Title fromTitle, Title toTitle)
		{
			var sb = new StringBuilder();
			var replacementsMade = false;
			var lines = match.Groups["content"].Value.Replace("\r", string.Empty).Split('\n');
			foreach (var line in lines)
			{
				var pageName = line.Split(TextArrays.Pipe, 2)[0];
				var title = new TitleParts(fromTitle.Site, pageName);
				if (fromTitle.PageName == title.PageName)
				{
					if (fromTitle.Namespace == MediaWikiNamespaces.File)
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

		private static string ReplaceLink(Match match, Page currentPage, Title from, Title to)
		{
			var link = new SiteLink(from.Site, match.Value);
			if (link.Namespace == from.Namespace && link.PageName == from.PageName)
			{
				if (link.DisplayParameter == null && currentPage.Namespace.IsTalkSpace)
				{
					var originalText = match.Value.Trim();
					originalText = originalText.Substring(2, originalText.Length - 4);
					link.DisplayText = originalText;
				}

				link.Namespace = to.Namespace;
				link.PageName = to.PageName;
				link.NormalizeDisplayText();

				return link.ToString();
			}

			return match.Value;
		}

		private static string ReplaceTemplate(Match match, Page currentPage, Title fromTitle, Title toTitle)
		{
			var site = currentPage.Site;
			var template = Template.Parse(match.Value);
			if (fromTitle.Namespace == MediaWikiNamespaces.Template && template.Name == fromTitle.PageName)
			{
				template.Name = toTitle.PageName;
			}
			else if (fromTitle.TextEquals(template.Name))
			{
				template.Name = toTitle.FullPageName;
			}

			var templateTitle = new TitleParts(site, template.Name, MediaWikiNamespaces.Template);
			if (site.UserFunctions.TemplateReplacements.TryGetValue(templateTitle, out var templateReplacement))
			{
				templateReplacement(currentPage, template, fromTitle, toTitle);
			}

			var retval = template.ToString();

			return retval;
		}
		#endregion

		#region Private Static Methods
		private static void ReplaceLinksAndTemplates(Page currentPage, Replacement replacement)
		{
			currentPage.Text = SiteLink.Find().Replace(currentPage.Text, (match) => ReplaceLink(match, currentPage, replacement.From, replacement.To));
			currentPage.Text = Template.Find().Replace(currentPage.Text, (match) => ReplaceTemplate(match, currentPage, replacement.From, replacement.To));

			// Galleries
			if (replacement.From.Namespace == MediaWikiNamespaces.File)
			{
				currentPage.Text = GalleryFinder.Replace(currentPage.Text, (match) => ReplaceGalleryLinks(match, replacement.From, replacement.To));
			}
		}
		#endregion

		#region Private Methods
		private void BacklinkLoaded(object sender, Page page)
		{
			foreach (var replacement in this.Replacements)
			{
				if (replacement.Action == ReplacementAction.Move || this.TaskActions.HasFlag(TaskActions.FixLinks))
				{
					ReplaceLinksAndTemplates(page, replacement);
				}

				this.CustomReplace(page, replacement);
			}

			this.CustomReplace(page);
		}
		#endregion
	}
}