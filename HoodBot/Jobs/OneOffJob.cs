namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffJob : WikiJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager) => this.Results = new PageResultHandler(this.Site, "User:HoodBot/ESO Books to Move");

		protected override void Main()
		{
			var lorePages = new TitleCollection(this.Site);
			lorePages.GetBacklinks("Template:Lore Book", BacklinksTypes.EmbeddedIn, false);
			lorePages.GetBacklinks("Template:Lore Book Compilation", BacklinksTypes.EmbeddedIn, false);

			var pageCreator = new MetaTemplateCreator("lorename");
			var bookPages = new PageCollection(this.Site, new PageLoadOptions(PageModules.Default | PageModules.Custom), pageCreator);
			bookPages.SetLimitations(LimitationType.FilterTo, new[] { UespNamespaces.Online });
			bookPages.GetBacklinks("Template:Game Book", BacklinksTypes.EmbeddedIn, false);
			bookPages.GetBacklinks("Template:Game Book Compilation", BacklinksTypes.EmbeddedIn, false);

			var excludeTitles = new TitleCollection(
				this.Site,
				"File:ON-icon-book-Note 01.png",
				"File:ON-icon-book-Note 02.png",
				"File:ON-icon-book-Note 03.png",
				"File:ON-icon-book-Note 04.png",
				"File:ON-icon-book-Note 05.png",
				"File:ON-icon-book-Scroll 01.png",
				"File:ON-icon-book-Scroll 02.png",
				"File:ON-icon-book-Scroll 03.png",
				"File:ON-icon-book-Scroll 04.png",
				"File:ON-icon-book-Scroll 05.png",
				"File:ON-icon-book-Scroll 06.png");

			var excludePages = excludeTitles.Load(PageModules.FileUsage);
			foreach (var page in excludePages)
			{
				foreach (var link in page.Backlinks)
				{
					bookPages.Remove(link.Key);
				}
			}

			for (var bookIndex = bookPages.Count - 1; bookIndex >= 0; bookIndex--)
			{
				var page = (VariablesPage)bookPages[bookIndex];
				if (page.GetVariable("lorename") is string loreName)
				{
					if (lorePages.Contains("Lore:" + loreName) ||
						(page.PageName.StartsWith("Crafting Motif", StringComparison.OrdinalIgnoreCase) &&
						!page.PageName.EndsWith("Style", StringComparison.OrdinalIgnoreCase)))
					{
						bookPages.RemoveAt(bookIndex);
					}
				}
			}

			excludeTitles.SetLimitations(LimitationType.FilterTo, UespNamespaces.Online);
			excludeTitles.Clear();
			excludeTitles.GetBacklinks("Template:Pre-Release", BacklinksTypes.EmbeddedIn);
			excludeTitles.GetCategoryMembers("Online-Books-No Collection");
			foreach (var title in excludeTitles)
			{
				bookPages.Remove(title);
			}

			var sizedPages = new List<(int Size, Page Title)>();
			foreach (var title in bookPages)
			{
				var parser = new ContextualParser(title);
				var gameBookIndex = parser.Nodes.FindIndex<SiteTemplateNode>(template => template.TitleValue.PageName.StartsWith("Game Book", StringComparison.OrdinalIgnoreCase));
				var textNodes = parser.Nodes.GetRange(gameBookIndex + 1, parser.Nodes.Count - gameBookIndex - 1);
				var postText = WikiTextVisitor.Raw(textNodes) ?? string.Empty;
				sizedPages.Add((postText.Length, title));
			}

			this.WriteLine("__TOC__");
			this.WriteLine("== Sorted Alphabetically ==");
			bookPages.Sort();
			foreach (var title in bookPages)
			{
				this.WriteLine("* " + title.AsLink(false));
			}

			this.WriteLine("\n== Sorted By Size ==");
			sizedPages.Sort((x, y) => -x.Size.CompareTo(y.Size));
			foreach (var title in sizedPages)
			{
				this.WriteLine("* " + title.Title.AsLink(false));
			}
		}

		protected override void JobCompleted()
		{
			base.JobCompleted();
			this.Results?.Save();
		}

		/*
		protected override string EditSummary => "Fix issue with Mod Header and Mod Icon Data on same page";

		protected override void LoadPages()
		{
			var iconData = new TitleCollection(this.Site);
			iconData.GetBacklinks("Template:Mod Icon Data", BacklinksTypes.EmbeddedIn);
			var modHeader = new TitleCollection(this.Site);
			modHeader.GetBacklinks("Template:Mod Header", BacklinksTypes.EmbeddedIn);

			var hashSet = new HashSet<ISimpleTitle>(iconData, SimpleTitleEqualityComparer.Instance);
			hashSet.IntersectWith(modHeader);

			this.Pages.GetTitles(hashSet);
		}

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			ThrowNull(parsedPage, nameof(parsedPage));
			if (parsedPage.FindTemplate("Mod Icon Data") is ITemplateNode iconTemplate &&
				parsedPage.FindTemplate("Mod Header") is ITemplateNode headerTemplate &&
				iconTemplate.Find(1) is IParameterNode icon)
			{
				headerTemplate.Add("icon", icon.ValueToText());
				parsedPage.Nodes.Remove(iconTemplate);
			}
		}
		*/
		#endregion
	}
}