namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class OneOffJob : EditJob
	{
		#region Private Constants
		private const string Ending = " (China).png";
		#endregion

		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override string EditSummary => "Update Chinese card art";

		protected override void LoadPages()
		{
			var galleryUpdates = this.UpdateChinesePages();
			var cardPages = new PageCollection(this.Site);
			cardPages.GetTitles(galleryUpdates.Keys);
			foreach (var kvp in galleryUpdates)
			{
				var cardPage = cardPages[kvp.Key];
				var parser = new ContextualParser(cardPage);
				var gallery = parser.Find<ITagNode>(t => string.Equals(t.Name, "gallery", StringComparison.OrdinalIgnoreCase));
				if (gallery is not null && gallery.InnerText is not null)
				{
					if (!gallery.InnerText.EndsWith('\n'))
					{
						gallery.InnerText += "\n";
					}

					foreach (var title in kvp.Value)
					{
						gallery.InnerText += title.PageName + "|Chinese card art\n";
					}

					parser.UpdatePage();
					this.Pages.Add(parser.Page);
				}
			}
		}

		protected override void PageLoaded(Page page) => throw new NotSupportedException();
		#endregion

		#region Private Static Methods
		private static void UpdateChinesePage(Page page, Page original)
		{
			var originalParser = new ContextualParser(original);
			var chineseParser = new ContextualParser(page);
			var sections = originalParser.ToSections(2);
			Section? si = null;
			foreach (var section in sections)
			{
				if (string.Equals(section.Header?.GetTitle(true), "Similar Images", StringComparison.Ordinal))
				{
					si = section;
					break;
				}
			}

			var chineseSections = chineseParser.ToSections(2);
			if (si is not null && chineseSections.Count == 2)
			{
				chineseSections.Insert(1, si);
			}
			else
			{
				throw new InvalidOperationException();
			}

			chineseParser.FromSections(chineseSections);
			var added = false;
			var offset = chineseParser.Count;
			foreach (var link in originalParser.LinkNodes)
			{
				if (link is SiteLinkNode sl && sl.TitleValue.Namespace == MediaWikiNamespaces.Category)
				{
					added = true;
					chineseParser.Add(link);
					chineseParser.AddText("\n");
				}
			}

			if (added)
			{
				chineseParser.Insert(offset, chineseParser.Factory.TextNode("\n\n"));
			}

			chineseParser.AddCategory("Legends-Card Art Images-Censored", true);
			chineseParser.UpdatePage();
		}
		#endregion

		#region Private Methods
		private Dictionary<Title, TitleCollection> UpdateChinesePages()
		{
			var artPages = new PageCollection(this.Site, PageModules.Backlinks | PageModules.Info | PageModules.Revisions);
			artPages.GetNamespace(MediaWikiNamespaces.File, CommonCode.Filter.Exclude, "LG-cardart-");
			artPages.Sort();
			var galleryTitles = new Dictionary<Title, TitleCollection>();
			foreach (var artPage in artPages)
			{
				if (artPage.Title.PageName.EndsWith(Ending, StringComparison.Ordinal))
				{
					var origName = artPage.Title.FullPageName()[0..^Ending.Length];
					_ = artPages.TryGetValue(origName + ".png", out var original) ||
					artPages.TryGetValue(origName + ".jpg", out original);
					if (original is not null)
					{
						UpdateChinesePage(artPage, original);
						this.Pages.Add(artPage);
						foreach (var backlink in original.Backlinks)
						{
							if (backlink.Value.HasFlag(BacklinksTypes.ImageUsage) &&
								backlink.Key.Namespace == UespNamespaces.Legends &&
								!backlink.Key.PageName.StartsWith("Card Art/", StringComparison.Ordinal) &&
								!backlink.Key.PageNameEquals("Card Lore"))
							{
								if (!galleryTitles.TryGetValue(backlink.Key, out var titles))
								{
									titles = new TitleCollection(this.Site);
									galleryTitles.Add(backlink.Key, titles);
								}

								titles.Add(artPage.Title);
							}
						}
					}
				}
			}

			return galleryTitles;
		}
		#endregion
	}
}