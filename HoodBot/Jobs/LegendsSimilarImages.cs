namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class LegendsSimilarImages : EditJob
	{
		#region Fields
		private readonly TitleCollection allTitles;
		private readonly Dictionary<Title, Title> primaryLookup;
		#endregion

		#region Constructors
		[JobInfo("Add Similar Images", "Legends")]
		public LegendsSimilarImages(JobManager jobManager)
			: base(jobManager)
		{
			this.allTitles = new TitleCollection(this.Site);
			this.primaryLookup = new Dictionary<Title, Title>(SimpleTitleComparer.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override Action<EditJob, Page>? EditConflictAction => null;

		protected override string EditSummary => "Add Similar Images";
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			var factory = new SiteNodeFactory(this.Site);
			var entryPages = PageCollection.Unlimited(this.Site);
			entryPages.GetNamespace(MediaWikiNamespaces.User, Filter.Exclude, "HoodBot/Legends Images");
			foreach (var entry in entryPages)
			{
				foreach (var section in entry.Text.Split("\n|-\n"))
				{
					var nodes = factory.Parse(section);
					Title? first = null;
					foreach (var linkNode in nodes.FindAll<SiteLinkNode>(null, false, false, 0))
					{
						var title = linkNode.TitleValue;
						this.allTitles.Add(title);
						if (first is null)
						{
							first = title;
						}
						else
						{
							if (!this.primaryLookup.TryAdd(title, first))
							{
								Debug.WriteLine($"\nDuplicate entries for {title.FullPageName}");
								Debug.WriteLine("  " + this.primaryLookup[title]);
								Debug.WriteLine("  " + first);
							}
						}
					}
				}
			}
		}

		protected override void LoadPages() => this.Pages.GetTitles(this.allTitles);

		protected override void PageLoaded(Page page)
		{
			page.Text = page.Text.Replace("==Licensing== {{esimage}}", "==Licensing==\n{{esimage}}", StringComparison.Ordinal);
			var title = this.primaryLookup[page];
			AddSimilarImages(page, title);
		}
		#endregion

		#region Private Methods
		private static void AddSimilarImages(Page page, Title? title)
		{
			var parser = new ContextualParser(page);
			if (parser.FindSiteTemplate("Similar Images") is null)
			{
				var sections = new List<Section>(parser.ToSections());
				var insertSection = 1;
				for (var i = 0; i < sections.Count; i++)
				{
					if (string.Equals(sections[i].Header?.GetTitle(true), "Summary", StringComparison.Ordinal))
					{
						insertSection = i + 1;
						break;
					}
				}

				var sectionHeader = parser.Factory.HeaderNodeFromParts(2, "Similar Images");
				var nodes = new NodeCollection(parser.Factory);
				var template = parser.Factory.TemplateNodeFromParts("Similar Images");
				if (title is not null)
				{
					template.Add(title.PageName);
				}

				sections[insertSection - 1].Content.AddText("\n\n");
				nodes.AddText("\n");
				nodes.Add(template);
				nodes.AddText("\n\n");
				var similarImages = new Section(sectionHeader, nodes);
				sections.Insert(insertSection, similarImages);
				parser.FromSections(sections);
				parser.UpdatePage();
				page.Text = page.Text.Replace("\n\n\n==Similar Images==", "\n\n==Similar Images==", StringComparison.Ordinal);
			}
		}
		#endregion
	}
}
