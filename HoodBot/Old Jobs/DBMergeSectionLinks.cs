﻿namespace RobinHood70.HoodBot.Jobs
{
	internal sealed class DBMergeSectionLinks : WikiJob
	{
		[JobInfo("Check Dragonborn Section Links")]
		public DBMergeSectionLinks(JobManager jobManager)
			: base(jobManager)
		{
			if (this.Results is PageResultHandler results)
			{
				results.Page = this.Site.CreatePage(MediaWikiNamespaces.User, "Kiz/Sandbox1", "{{#addtotrail:[[User:Kiz|Kiz]]: [[User:Kiz/Subpages|Subpages]]}}{{Notice|<onlyinclude>DBMerge - Outstanding Links</onlyinclude>}}\n----");
			}
		}

		protected override void Main()
		{
			TitleCollection titles = new(this.Site);
			SimpleTitleJsonConverter titleConverter = new(this.Site);
			var repFile = File.ReadAllText(UespSite.GetBotDataFolder("Replacements - Merge.json"));
			var reps = JsonConvert.DeserializeObject<IEnumerable<Replacement<Title, Title>>>(repFile, titleConverter) ?? throw new InvalidOperationException();
			foreach (var rep in reps)
			{
				titles.Add(rep.To);
			}

			titles.Remove("Skyrim:Miscellaneous Items"); // This one is legitimately #Dragonborn

			PageCollection pages = new(this.Site, PageModules.LinksHere);
			pages.GetTitles(titles);

			TitleCollection backlinks = new(this.Site);
			foreach (var page in pages)
			{
				foreach (var item in page.Backlinks)
				{
					backlinks.Add(item.Key);
				}
			}

			backlinks.Remove("User:HoodBot/Results");
			backlinks.Remove("User:Kiz/Sandbox1");
			//// backlinks.Sort(); // Only useful for debugging

			pages = PageCollection.Unlimited(this.Site);
			pages.GetTitles(backlinks);
			pages.Sort();
			foreach (var page in pages)
			{
				SiteParser parser = new(page);
				foreach (var node in parser.LinkNodes)
				{
					FullTitle link = new(TitleFactory.FromBacklinkNode(this.Site, node));
					if (string.Equals(link.Fragment, "Dragonborn", StringComparison.Ordinal) && titles.Contains(link))
					{
						this.WriteLine($"* {page.AsLink()}: {WikiTextVisitor.Raw(node)}");
					}
				}
			}
		}
	}
}