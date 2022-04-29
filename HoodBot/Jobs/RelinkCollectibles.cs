namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class RelinkCollectibles : MovePagesJob
	{
		#region Fields
		private readonly List<Title> esoTitles = new();
		private readonly Dictionary<Title, string> disambigs = new(SimpleTitleComparer.Instance);
		#endregion

		#region Constructors
		[JobInfo("Relink ESO Collectibles", "ESO")]
		public RelinkCollectibles(JobManager jobManager)
				: base(jobManager)
		{
			this.MoveAction = MoveAction.None;
			this.EditSummaryMove = "Match page name to item";
			this.AllowFromEqualsTo = true; // Replacements are dummy replacements to trigger GetToLink();
		}
		#endregion

		#region Protected Override Methods

		protected override SiteLink GetToLink(Title page, bool isRedirectTarget, SiteLink from, Title to)
		{
			if (from.Interwiki == null && from.Fragment != null)
			{
				var newTitle =
					this.esoTitles.Find(title => title.PageNameEquals(from.Fragment + this.disambigs[to])) ??
					this.esoTitles.Find(title => title.PageNameEquals(from.Fragment));
				if (newTitle == null)
				{
					this.WriteLine($"* Match not found for {from} on page {page.AsLink()}.");
					return from;
				}

				var retval = from.WithTitle(TitleFactory.FromUnvalidated(to.Namespace, newTitle.PageName));
				retval.Fragment = null;
				if (this.GetLinkText(page, from, retval, !isRedirectTarget) is string newText)
				{
					retval.Text = newText;
				}

				return retval;
			}

			return from;
		}

		protected override void PopulateMoves()
		{
			var allTitles = new TitleCollection(this.Site);
			allTitles.GetNamespace(UespNamespaces.Online);
			allTitles.Sort();
			this.esoTitles.AddRange(allTitles);

			// var collectionTitles = this.GetCollectionTitles();
			var collectionTitles = GetTitles();
			var collectionPages = this.GetPages(collectionTitles);
			this.PopulateFromCollections(collectionPages);
		}

		protected override void UpdateLinkNode(Page page, ILinkNode node, bool isRedirectTarget)
		{
			page.ThrowNull();
			node.ThrowNull();
			var from = SiteLink.FromLinkNode(this.Site, node);
			if (this.LinkUpdateMatch(from) is Title to)
			{
				var toLink = this.GetToLink(page, isRedirectTarget, from, to);
				if (!from.FullEquals(toLink))
				{
					toLink.UpdateLinkNode(node);
				}
			}

			if (from.Namespace == MediaWikiNamespaces.Media)
			{
				Title key = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.File], from.PageName);
				if (this.LinkUpdates.TryGetValue(key, out var toMedia))
				{
					var toLink = this.GetToLink(page, isRedirectTarget, from, TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Media], toMedia!.PageName));
					if (!from.FullEquals(toLink))
					{
						toLink.UpdateLinkNode(node);
					}
				}
			}
		}
		#endregion

		#region Private Static Methods
		/*
		private IEnumerable<string> GetCollectionTitles()
		{
			Regex CollectionFinder = new(@"^\*+'''\[\[(?<page>.*?)\|.*?\]\]'''$", RegexOptions.ExplicitCapture | RegexOptions.Multiline, Globals.DefaultRegexTimeout);

			var text = this.Site.LoadPageText("Online:Collections") ?? throw new InvalidOperationException();
			foreach (Match match in CollectionFinder.Matches(text))
			{
				var retval = match.Groups["page"];
				if (retval.Success)
				{
					yield return retval.Value;
				}
			}
		}
		*/

		private static IEnumerable<string> GetTitles() => new List<string> { "Online:Antiquity Furnishings", "Online:Antique Maps" }; // GetCollectionTitles();
		#endregion

		#region Private Methods
		private PageCollection GetPages(IEnumerable<string> collectionTitles)
		{
			var plm = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Template], "PageLetterMenu");
			var collectionPages = new PageCollection(this.Site, PageModules.Templates);
			collectionPages.GetTitles(collectionTitles);

			var allTitles = new TitleCollection(this.Site);
			foreach (var page in collectionPages)
			{
				var disambig = page.PageName switch
				{
					"Mementos (collection)" => " (memento)",
					"Mounts" => " (mount)",
					"Personalities" => " (personality)",
					"Pets" => " (pet)",
					"Skins" => " (skin)",
					_ => string.Empty
				};

				if (page.Templates.Contains(plm, SimpleTitleComparer.Instance))
				{
					for (var c = 'A'; c <= 'Z'; c++)
					{
						var title = page.FullPageName + ' ' + c;
						allTitles.Add(title);
						this.disambigs.Add(TitleFactory.FromValidated(this.Site, title), disambig);
					}
				}
				else
				{
					allTitles.Add(page.FullPageName);
					this.disambigs.Add(TitleFactory.FromValidated(this.Site, page.FullPageName), disambig);
				}
			}

			var retval = new PageCollection(this.Site, PageModules.None);
			retval.GetTitles(allTitles);
			retval.RemoveExists(false);
			retval.Sort();

			return retval;
		}

		private void PopulateFromCollections(PageCollection collections)
		{
			foreach (var title in collections)
			{
				this.AddReplacement(title, title, ReplacementActions.None, "dummy update");
			}
		}
		#endregion
	}
}