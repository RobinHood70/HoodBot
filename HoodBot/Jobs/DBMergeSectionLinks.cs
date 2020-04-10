namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using Newtonsoft.Json;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon.Parser;

	public class DBMergeSectionLinks : WikiJob
	{
		[JobInfo("Check Dragonborn Section Links")]
		public DBMergeSectionLinks(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}

		protected override void Main()
		{
			var titles = new TitleCollection(this.Site);
			var titleConverter = new ISimpleTitleJsonConverter(this.Site);
			var repFile = File.ReadAllText(Environment.ExpandEnvironmentVariables(@"%BotData%\Replacements - Merge.json"));
			var reps = JsonConvert.DeserializeObject<IEnumerable<Replacement>>(repFile, titleConverter) ?? throw new InvalidOperationException();
			foreach (var rep in reps)
			{
				titles.Add(rep.To);
			}

			var pages = new PageCollection(this.Site, PageModules.LinksHere);
			pages.GetTitles(titles);

			var backlinks = new TitleCollection(this.Site);
			foreach (var page in pages)
			{
				foreach (var item in page.Backlinks)
				{
					backlinks.Add(item.Key);
				}
			}

			backlinks.Sort();
			backlinks.Remove("User:HoodBot/Results");

			pages = PageCollection.Unlimited(this.Site);
			//// pages.GetTitles("Category:Dragonborn-Factions-DLC2AshSpawnFaction");
			pages.GetTitles(backlinks);
			pages.Sort();
			foreach (var page in pages)
			{
				var parser = WikiTextParser.Parse(page.Text);
				foreach (var node in parser.FindAllRecursive<LinkNode>())
				{
					var linkTitle = WikiTextVisitor.Value(node.Title);
					var link = new FullTitle(this.Site, linkTitle);
					if (link.Fragment == "Dragonborn" && titles.Contains(link))
					{
						this.WriteLine($"* {page.AsLink(false)}: {WikiTextVisitor.Raw(node)}");
					}
				}
			}
		}
	}
}