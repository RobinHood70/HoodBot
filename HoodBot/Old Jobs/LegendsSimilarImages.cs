namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
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
			this.primaryLookup = [];
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			var factory = new WikiNodeFactory();
			var entryPages = PageCollection.Unlimited(this.Site);
			entryPages.GetNamespace(MediaWikiNamespaces.User, Filter.Exclude, "HoodBot/Legends Images");
			foreach (var entry in entryPages)
			{
				foreach (var section in entry.Text.Split("\n|-\n"))
				{
					var nodes = factory.Parse(section);
					var collection = new WikiNodeCollection(factory, nodes);
					Title? first = null;
					foreach (var linkNode in collection.FindAll<ILinkNode>(null, false, false, 0))
					{
						var title = linkNode.Title;
						this.allTitles.Add(title);
						if (first is not Title firstTitle)
						{
							first = title;
						}
						else if (!this.primaryLookup.TryAdd(title, firstTitle))
						{
							Debug.WriteLine($"\nDuplicate entries for {title.FullPageName()}");
							Debug.WriteLine("  " + this.primaryLookup[title]);
							Debug.WriteLine("  " + first);
						}
					}
				}
			}
		}

		protected override string GetEditSummary(Page page) => "Add Similar Images";

		protected override void LoadPages() => this.Pages.GetTitles(this.allTitles);

		protected override void PageLoaded(Page page)
		{
			page.Text = page.Text.Replace("==Licensing== {{esimage}}", "==Licensing==\n{{esimage}}", StringComparison.Ordinal);
			var title = this.primaryLookup[page.Title];
			AddSimilarImages(page, title);
		}
		#endregion

		#region Private Methods
		private static void AddSimilarImages(Page page, Title title)
		{
			var parser = new SiteParser(page);
			if (parser.FindTemplate("Similar Images") is null)
			{
				var sections = parser.ToSections();
				var insertSection = sections.IndexOf("Summary") + 1;
				if (insertSection == 0)
				{
					insertSection = 1;
				}

				var sectionHeader = parser.Factory.HeaderNodeFromParts(2, "Similar Images");
				var template = parser.Factory.TemplateNodeFromParts("Similar Images", true, (null, title.PageName));
				var nodes = new WikiNodeCollection(parser.Factory);
				nodes.AddText("\n");
				nodes.Add(template);
				nodes.AddText("\n\n");
				var similarImages = new Section(sectionHeader, nodes);
				sections.InsertWithSpaceBefore(insertSection, similarImages);
				parser.FromSections(sections);
				parser.UpdatePage();
				page.Text = page.Text.Replace("\n\n\n==Similar Images==", "\n\n==Similar Images==", StringComparison.Ordinal);
			}
		}
		#endregion
	}
}