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

	public class FurnishingSplit : ParsedPageJob
	{
		#region Constants
		private const string FurnishingPrefix = "ON-item-furnishing-";
		#endregion

		#region Fields
		private readonly List<string> issues = new();
		private PageCollection? existingPages;
		#endregion

		#region Constructors
		[JobInfo("Furnishing Split")]
		public FurnishingSplit(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary => "Create furniture page";
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

			PageCollection allPages = new(this.Site, PageLoadOptions.None);
			allPages.GetTitles(allTitles);
			allPages.RemoveExists(false);
			this.existingPages = allPages;
		}

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

		protected override void ParseText(object sender, ContextualParser parser)
		{
			parser.ThrowNull();
			parser.Page.PropertyThrowNull(nameof(parser));
			List<string> pageIssues = new();
			if (!parser.Page.PageName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
			{
				pageIssues.Add("does not end in .jpg");
			}

			if (parser.FindSiteTemplate("Furnishing Summary") == null)
			{
				pageIssues.Add("does not have a Furnishing Summary");
			}

			var title = PageFromFile(parser.Page);
			if (this.existingPages!.Contains(title))
			{
				pageIssues.Add($"page exists: {title.AsLink(LinkFormat.LabelName)}");
			}

			if (pageIssues.Count > 0)
			{
				this.issues.Add($"* {parser.Page.AsLink()}: {string.Join(", ", pageIssues)}.");
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

			return TitleFactory.FromUnvalidated(page.Namespace.Site[UespNamespaces.Online], pageName);
		}
		#endregion
	}
}
