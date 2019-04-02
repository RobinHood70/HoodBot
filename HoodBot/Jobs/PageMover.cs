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

	#region Internal Enums
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
		Default = CheckLinksRemaining | CheckToExists | FixLinks | Move | MoveTalkPage | MoveSubpages | SuppressRedirect | SaveResults,
	}
	#endregion

	public abstract class PageMover : EditJob
	{
		#region Static Fields
		private readonly PageCollection backlinks;
		private static Regex galleryFinder = new Regex(@"(?<open><gallery(\ [^>]*?)?>\s*\n)(?<content>.*?)(?<close>\s*\n</gallery>)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
		private bool hasMoves;
		#endregion

		#region Constructors
		protected PageMover(Site site, AsyncInfo asyncInfo, TaskActions taskActions)
			: base(site, asyncInfo)
		{
			this.TaskActions = taskActions;
			this.TalkLikePages = new TitleCollection(site);

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

		#region Protected Static Properties
		protected static string ReplacementStatusFile => @"%BotData%\Replacements.json";
		#endregion

		#region Protected Properties
		protected bool DoReport { get; set; } = true;

		protected string EditSummaryEditAfterMove { get; set; } = "Update text after page move";

		protected string EditSummaryMove { get; set; } = "Move page";

		protected string EditSummaryUpdateLinks { get; set; } = "Update links after page move";

		protected IList<Replacement> Replacements { get; private set; } = new List<Replacement>();

		protected TitleCollection TalkLikePages { get; }

		protected TaskActions TaskActions { get; set; } = TaskActions.Default;
		#endregion

		#region Protected Methods
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Optiona, to be called only when necessary.")]
		protected void LoadReplacementsFromFile(string fileName)
		{
			var repFile = File.ReadLines(fileName);
			foreach (var line in repFile)
			{
				var rep = line.Split('\t');
				this.Replacements.Add(new Replacement(this.Site, rep[0].Trim(), rep[1].Trim()));
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			if (this.hasMoves)
			{
				if (this.TaskActions.HasFlag(TaskActions.Move) || this.TaskActions.HasFlag(TaskActions.ProposeUnused))
				{
					this.DoMoves();
				}

				if (this.TaskActions.HasFlag(TaskActions.FixLinks))
				{
					this.DoFixLinks(this.backlinks);
				}
			}

			if (this.Site.EditingEnabled && this.TaskActions.HasFlag(TaskActions.CheckLinksRemaining))
			{
				this.DoPostCheck();
			}

			File.Delete(ReplacementStatusFile);
		}

		protected override void PrepareJob()
		{
			this.TalkLikePages.GetCategoryMembers("Message Boards", false);
			this.PopulateReplacementsInternal();
			this.FigureOutWhatToDo();
			if (this.TaskActions.HasFlag(TaskActions.SaveResults))
			{
				this.EmitReport();
			}
		}
		#endregion

		#region Protected Abstract Methods
		protected abstract IList<Replacement> GetReplacements();
		#endregion

		#region Protected Virtual Methods
		protected virtual void CustomReplace(Page page)
		{
		}

		protected virtual void CustomReplace(Page page, Replacement replacement)
		{
		}

		protected virtual void DoFixLinks(PageCollection backlinks)
		{
			this.StatusWriteLine("Replacing links");
			this.ProgressMaximum = backlinks.Count + 1;
			foreach (var page in backlinks)
			{
				foreach (var replacement in this.Replacements)
				{
					if (replacement.Action == ReplacementAction.Move)
					{
						this.ReplaceLinksAndTemplates(
							page,
							replacement);
						this.CustomReplace(page, replacement);
					}
				}

				this.CustomReplace(page);
				page.Save(this.EditSummaryUpdateLinks, true);

				this.Progress++;
			}
		}

		protected virtual void DoMoves()
		{
			var needPages = new TitleCollection(this.Site);
			this.StatusWriteLine("Moving pages and proposing deletions");
			this.ProgressMaximum = this.Replacements.Count;
			foreach (var replacement in this.Replacements)
			{
				switch (replacement.Action)
				{
					case ReplacementAction.Move:
						if (this.TaskActions.HasFlag(TaskActions.Move))
						{
							replacement.From.Move(replacement.To.FullPageName, this.EditSummaryMove, this.TaskActions.HasFlag(TaskActions.MoveTalkPage), this.TaskActions.HasFlag(TaskActions.MoveSubpages), this.TaskActions.HasFlag(TaskActions.SuppressRedirect));
							if (this.TaskActions.HasFlag(TaskActions.EditAfterMove))
							{
								needPages.Add(replacement.To);
							}
						}

						if (this.TaskActions.HasFlag(TaskActions.ProposeRedirects) && !this.TaskActions.HasFlag(TaskActions.SuppressRedirect))
						{
							needPages.Add(replacement.From.FullPageName);
						}

						break;
					case ReplacementAction.ProposeForDeletion:
						needPages.Add(replacement.From.FullPageName);
						break;
				}

				this.Progress++;
			}

			if (needPages.Count > 0)
			{
				var pages = PageCollection.Unlimited(this.Site);
				pages.GetTitles(needPages);
				foreach (var replacement in this.Replacements)
				{
					switch (replacement.Action)
					{
						case ReplacementAction.Move:
							if (this.TaskActions.HasFlag(TaskActions.ProposeRedirects) && !this.TaskActions.HasFlag(TaskActions.SuppressRedirect))
							{
								this.ProposeForDeletion(pages[replacement.From.FullPageName], "Redirect from page move.");
							}

							if (this.TaskActions.HasFlag(TaskActions.Move | TaskActions.EditAfterMove))
							{
								var title = this.Site.EditingEnabled ? replacement.To : replacement.From;
								var page = pages[title.FullPageName];
								this.EditAfterMove(page, replacement);
							}

							break;
						case ReplacementAction.ProposeForDeletion:
							this.ProposeForDeletion(pages[replacement.From.FullPageName], "Not linked or transcluded.");
							break;
					}
				}
			}
		}

		protected virtual void DoPostCheck()
		{
			this.StatusWriteLine("Checking remaining pages");
			this.ProgressMaximum = this.Replacements.Count;
			var leftovers = new List<Title>();
			foreach (var rep in this.Replacements)
			{
				if (rep.Action == ReplacementAction.Move)
				{
					var backlinks = new TitleCollection(this.Site);
					backlinks.GetBacklinks(rep.To.FullPageName, BacklinksTypes.All, true);
					backlinks.Remove(rep.To);
					this.FilterSitePages(backlinks);
					if (backlinks.Count > 0)
					{
						leftovers.Add(rep.To);
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

		/// <summary>This function allows additional edits after the move.</summary>
		/// <param name="page">The page to edit. This will be the destination page when AllowWrites = true; otherwise, it will be the source page.</param>
		/// <param name="replacement">The replacement object. This is included in case the From name is needed or the Replacement is a derived type.</param>
		protected virtual void EditAfterMove(Page page, Replacement replacement)
		{
		}

		protected virtual void EmitReport()
		{
			this.WriteLine("{| class=\"wikitable sortable\"");
			this.WriteLine("! Page !! Action");
			foreach (var rep in this.Replacements)
			{
				this.WriteLine("|-");
				this.Write(Globals.Invariant($"| {rep.From} ([[Special:WhatLinksHere/{rep.From}|links]]) || "));

				switch (rep.Action)
				{
					case ReplacementAction.Unknown:
						this.Write("Unknown! This should never happen.");
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

		protected virtual void FigureOutWhatToDo()
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
			}

			var backlinkTitles = new TitleCollection(this.Site);
			foreach (var replacement in this.Replacements)
			{
				if (this.TaskActions.HasFlag(TaskActions.ProposeUnused) || this.TaskActions.HasFlag(TaskActions.FixLinks))
				{
					var count = backlinkTitles.Count;
					backlinkTitles.GetBacklinks(replacement.From.FullPageName, BacklinksTypes.All);
					if (this.TaskActions.HasFlag(TaskActions.ProposeUnused) && backlinkTitles.Count == count)
					{
						var prodResult = fromPages.TryGetValue(replacement.From.FullPageName, out var fromPage) ? this.CanDelete(fromPage) : ProposedDeletionResult.PageLoadError;
						switch (prodResult)
						{
							case ProposedDeletionResult.AlreadyProposed:
								replacement.Action = ReplacementAction.Skip;
								replacement.ActionReason = "Already proposed for deletion";
								break;
							case ProposedDeletionResult.FoundNoDeleteRequest:
								replacement.Action = ReplacementAction.Move;
								replacement.ActionReason = "No links, but {{tl|Linked image}} present - move instead of proposing for deletion";
								break;
							case ProposedDeletionResult.NonExistent:
							case ProposedDeletionResult.PageLoadError:
								throw new ArgumentNullException($"Page doesn't exist or failed to load: {replacement.From}");
							case ProposedDeletionResult.Add:
								replacement.Action = ReplacementAction.ProposeForDeletion;
								replacement.ActionReason = "no links";
								break;
						}
					}
				}

				if (replacement.Action == ReplacementAction.Unknown)
				{
					if (this.TaskActions.HasFlag(TaskActions.CheckToExists) && toPages.Contains(replacement.To))
					{
						replacement.Action = ReplacementAction.Skip;
						replacement.ActionReason = string.Format($"{SiteLink.LinkTextFromTitle(replacement.To)} exists");
					}
					else
					{
						replacement.Action = ReplacementAction.Move;
					}
				}

				this.hasMoves |= replacement.Action == ReplacementAction.Move;
				this.Progress++;
			}

			this.FilterBacklinks(backlinkTitles);
			this.FilterSitePages(backlinkTitles);
			this.backlinks.GetTitles(backlinkTitles);
			this.backlinks.Sort();
		}

		protected virtual void FilterBacklinks(TitleCollection backlinkTitles) => backlinkTitles.RemoveNamespaces(
				MediaWikiNamespaces.Main,
				MediaWikiNamespaces.Media,
				MediaWikiNamespaces.MediaWiki,
				MediaWikiNamespaces.Project,
				MediaWikiNamespaces.Special,
				MediaWikiNamespaces.Template);

		protected virtual void FilterSitePages(TitleCollection backlinkTitles)
		{
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

			return replacementsMade ? match.Groups["pre"].Value.Trim() + "\n" + sb.ToString() + match.Groups["post"].Value.Trim() : match.Value;
		}

		private static string ReplaceLink(Match match, Title from, Title to)
		{
			var link = new SiteLink(from.Site, match.Value);
			if (link.Namespace == from.Namespace && link.PageName == from.PageName)
			{
				if (link.DisplayParameter == null)
				{
					link.DisplayText = link.ToString();
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
			var template = new Template(match.Value);
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

			return template.ToString();
		}
		#endregion

		#region Private Methods
		private void PopulateReplacementsInternal()
		{
			this.StatusWriteLine("Getting Replacement List");
			if (File.Exists(ReplacementStatusFile))
			{
				var repsFile = File.ReadAllText(ReplacementStatusFile);
				this.Replacements = JsonConvert.DeserializeObject<List<Replacement>>(repsFile);
			}
			else
			{
				this.Replacements = this.GetReplacements();
				var newFile = JsonConvert.SerializeObject(this.Replacements, Formatting.Indented);
				File.WriteAllText(ReplacementStatusFile, newFile);
			}
		}

		private void ReplaceLinksAndTemplates(Page currentPage, Replacement replacement)
		{
			currentPage.Text = SiteLink.Find().Replace(currentPage.Text, (match) => ReplaceLink(match, replacement.From, replacement.To));
			currentPage.Text = Template.Find().Replace(currentPage.Text, (match) => ReplaceTemplate(match, currentPage, replacement.From, replacement.To));

			// Galleries
			if (replacement.From.Namespace == MediaWikiNamespaces.File)
			{
				currentPage.Text = galleryFinder.Replace(currentPage.Text, (match) => ReplaceGalleryLinks(match, replacement.From, replacement.To));
			}
		}
		#endregion
	}
}