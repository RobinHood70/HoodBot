﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class SplitToAlphabetic : EditJob
	{
		#region Fields
		private readonly Title mainPage;
		private readonly HashSet<string> skipSections = new(StringComparer.OrdinalIgnoreCase)
		{
			string.Empty,
			"Gallery",
			"Notes",
			"References"
		};

		private readonly Dictionary<string, string> replacements = new(StringComparer.Ordinal)
		{
			["Siltwallow Burrfish"] = "D",
			["Pinegilled Drum"] = "Drum, Pinegilled",
			["Bluespotted Cornetfish"] = "Cornetfish, Bluespotted",
			["Gonfalon Rockfish"] = "Rockfish, Gonfalon",
			["[[Lore:Slaughterfish|Slaughterfish]]"] = "Slaughterfish"
		};

		[JobInfo("Split to Alphabetic")]
		public SplitToAlphabetic(JobManager jobManager)
			: base(jobManager)
		{
			this.mainPage = TitleFactory.FromUnvalidated(this.Site, "Lore:Fish");
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeMain()
		{
			base.BeforeMain();
			this.SplitMainPage();
		}

		protected override string GetEditSummary(Page page) => "Update links after page split";

		protected override void LoadPages() => this.Pages.GetBacklinks(this.mainPage.FullPageName());

		protected override void PageLoaded(Page page)
		{
			var parser = new SiteParser(page);
			foreach (var link in parser.LinkNodes)
			{
				var linkTitle = TitleFactory.FromBacklinkNode(this.Site, link);
				if (linkTitle.Title == this.mainPage && linkTitle.Fragment?.Length > 0)
				{
					var fragment = linkTitle.Fragment.Trim();
					if (this.skipSections.Contains(fragment))
					{
						continue;
					}

					var sortTitle = this.replacements.GetValueOrDefault(fragment, fragment);
					var letter = sortTitle[0];
					link.TitleNodes.Clear();
					link.TitleNodes.AddText($"{linkTitle.FullPageName()} {letter}#{fragment}");
				}
			}

			parser.UpdatePage();
		}
		#endregion

		#region Private Methods
		private Page CreatePage(KeyValuePair<char, List<Section>> entry)
		{
			Page page;
			var pageTitle = "Lore:Fish " + entry.Key;
			page = this.Site.CreatePage(pageTitle);
			var newParser = new SiteParser(page);
			newParser.FromSections(entry.Value);
			var refTemplates = newParser.FindSiteTemplates("Ref");
			bool hasRefs = false;
			bool hasUol = false;
			foreach (var template in refTemplates)
			{
				hasRefs = true;
				if (template.Find("group") is not null)
				{
					hasUol = true;
				}
			}

			newParser.InsertText(0, "{{Lore Fish Trail}}\n\n");
			newParser.TrimEnd();
			if (hasRefs)
			{
				newParser.AddParsed("\n\n==References==\n<references/>");
				if (hasUol)
				{
					newParser.AddParsed("\n{{UOL}}\n<references group=UOL/>");
				}
			}

			newParser.UpdatePage();
			return page;
		}

		private SortedList<char, List<Section>> GroupSections(IList<Section> sections)
		{
			var newSections = new SortedList<char, List<Section>>();
			foreach (var section in sections)
			{
				var sectionTitle = section.Header.GetTitle(true);
				if (!this.skipSections.Contains(sectionTitle))
				{
					var sortTitle = this.replacements.GetValueOrDefault(sectionTitle, sectionTitle);
					if (!newSections.TryGetValue(sortTitle[0], out var sectionList))
					{
						sectionList = [];
						newSections.Add(sortTitle[0], sectionList);
					}

					sectionList.Add(section);
				}
			}

			return newSections;
		}

		private void SplitMainPage()
		{
			if (this.Site.LoadPage(this.mainPage) is not Page page)
			{
				throw new InvalidOperationException();
			}

			var parser = new SiteParser(page);
			var sections = parser.ToSections(2);
			var newSections = this.GroupSections(sections);
			this.Progress = 0;
			this.ProgressMaximum = newSections.Count;
			foreach (var entry in newSections)
			{
				var newPage = this.CreatePage(entry);
				this.SavePage(newPage, "Create alphabetic page", false);
				this.Progress++;
			}
		}
		#endregion
	}
}
