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
		private readonly Dictionary<Title, string> disambigs = new();
		#endregion

		#region Constructors
		[JobInfo("Relink ESO Collectibles", "ESO")]
		public RelinkCollectibles(JobManager jobManager)
				: base(jobManager, false)
		{
			this.MoveAction = MoveAction.None;
			this.EditSummaryMove = "Match page name to item";
			this.AllowFromEqualsTo = true; // Replacements are dummy replacements to trigger GetToLink();
		}
		#endregion

		#region Protected Override Methods

		protected override SiteLink GetToLink(Page page, bool isRedirectTarget, SiteLink from, Title to)
		{
			if (from.Interwiki == null && from.Fragment != null)
			{
				var newTitle = this.esoTitles.Find(title => title.PageNameEquals(from.Fragment + this.disambigs[to]));
				if (newTitle.Namespace is null)
				{
					newTitle = this.esoTitles.Find(title => title.PageNameEquals(from.Fragment));
				}

				if (newTitle.Namespace is null)
				{
					this.WriteLine($"* Match not found for {from} on page {SiteLink.ToText(page, LinkFormat.Plain)}.");
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
			this.esoTitles.AddRange(allTitles.Titles());

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

			if (from.Title.Namespace == MediaWikiNamespaces.Media)
			{
				Title key = TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.File], from.Title.PageName);
				if (this.LinkUpdates.TryGetValue(key, out var toMedia))
				{
					var toLink = this.GetToLink(page, isRedirectTarget, from, TitleFactory.FromValidated(this.Site[MediaWikiNamespaces.Media], toMedia.Title.PageName));
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
				var title = page.Title;
				var disambig = title.PageName switch
				{
					"Mementos (collection)" => " (memento)",
					"Mounts" => " (mount)",
					"Personalities" => " (personality)",
					"Pets" => " (pet)",
					"Skins" => " (skin)",
					_ => string.Empty
				};

				if (page.Templates.Contains(plm))
				{
					for (var c = 'A'; c <= 'Z'; c++)
					{
						var indexedTitle = title.PageName + ' ' + c;
						allTitles.Add(title.Namespace.DecoratedName() + indexedTitle);
						this.disambigs.Add(TitleFactory.FromValidated(title.Namespace, indexedTitle), disambig);
					}
				}
				else
				{
					allTitles.Add(page.Title.FullPageName());
					this.disambigs.Add(TitleFactory.FromValidated(title.Namespace, title.PageName), disambig);
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
				this.AddReplacement(title.Title, title, ReplacementActions.None, "dummy update");
			}
		}
		#endregion
	}
}