namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;

	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	[method: JobInfo("Furnishing Split")]
	public class FurnishingSplit(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Constants
		private const string FurnishingPrefix = "ON-item-furnishing-";
		#endregion

		#region Fields
		private readonly List<string> issues = [];
		private PageCollection? existingPages;
		#endregion

		#region Protected Override Methods
		protected override void BeforeLoadPages()
		{
			TitleCollection furnishingFiles = new(this.Site);
			furnishingFiles.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, FurnishingPrefix);
			TitleCollection allTitles = new(this.Site);
			foreach (var page in furnishingFiles)
			{
				allTitles.Add(PageFromFile(page));
			}

			var allPages = PageCollection.Unlimited(this.Site);
			allPages.GetTitles(allTitles);
			allPages.RemoveExists(false);
			this.existingPages = allPages;
		}

		protected override string GetEditSummary(Page page) => "Create furniture page";

		protected override void LoadPages() => this.Pages.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, FurnishingPrefix);

		protected override void Main()
		{
			this.issues.Sort(StringComparer.Ordinal);
			foreach (var issue in this.issues)
			{
				this.WriteLine(issue);
			}

			base.Main();
		}

		protected override void ParseText(SiteParser parser)
		{
			ArgumentNullException.ThrowIfNull(parser);
			List<string> pageIssues = [];
			if (!parser.Page.Title.PageName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
			{
				pageIssues.Add("does not end in .jpg");
			}

			if (parser.FindSiteTemplate("Furnishing Summary") == null)
			{
				pageIssues.Add("does not have a Furnishing Summary");
			}

			var title = PageFromFile(parser.Page.Title);
			if (this.existingPages!.Contains(title))
			{
				pageIssues.Add($"page exists: {SiteLink.ToText(title, LinkFormat.LabelName)}");
			}

			if (pageIssues.Count > 0)
			{
				this.issues.Add($"* {SiteLink.ToText(parser.Page)}: {string.Join(", ", pageIssues)}.");
			}
		}
		#endregion

		#region Private Methods
		private static Title PageFromFile(Title page)
		{
			var pageName = page.PageName[FurnishingPrefix.Length..];
			var extension = pageName.LastIndexOf('.');
			if (extension >= 0)
			{
				pageName = pageName[..extension];
			}

			return TitleFactory.FromUnvalidated(page.Site[UespNamespaces.Online], pageName);
		}
		#endregion
	}
}