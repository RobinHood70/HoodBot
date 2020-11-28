namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	public class FurnishingSplit : ParsedPageJob
	{
		#region Constants
		private const string FurnishingPrefix = "ON-item-furnishing-";
		#endregion

		#region Fields
		private readonly List<string> issues = new List<string>();
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
		protected override void BeforeLogging()
		{
			var furnishingFiles = new TitleCollection(this.Site);
			furnishingFiles.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, FurnishingPrefix);
			var allTitles = new TitleCollection(this.Site);
			foreach (var page in furnishingFiles)
			{
				allTitles.Add(PageFromFile(page));
			}

			var allPages = new PageCollection(this.Site, PageLoadOptions.None);
			allPages.GetTitles(allTitles);
			allPages.RemoveExists(false);
			this.existingPages = allPages;
			base.BeforeLogging();
		}

		protected override void LoadPages() => this.Pages.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, FurnishingPrefix);

		protected override void Main()
		{
			this.issues.Sort();
			foreach (var issue in this.issues)
			{
				this.WriteLine(issue);
			}

			base.Main();
		}

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			ThrowNull(parsedPage, nameof(parsedPage));
			ThrowNull(parsedPage.Context, nameof(parsedPage), nameof(parsedPage.Context));
			var pageIssues = new List<string>();
			if (!parsedPage.Context.PageName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
			{
				pageIssues.Add("does not end in .jpg");
			}

			if (parsedPage.FindTemplate("Furnishing Summary") == null)
			{
				pageIssues.Add("does not have a Furnishing Summary");
			}

			var title = PageFromFile(parsedPage.Context);
			if (this.existingPages!.Contains(title))
			{
				pageIssues.Add($"page exists: {title.AsLink(true)}");
			}

			if (pageIssues.Count > 0)
			{
				this.issues.Add($"* {parsedPage.Context.AsLink(false)}: {string.Join(", ", pageIssues)}.");
			}
		}
		#endregion

		#region Private Methods
		private static ISimpleTitle PageFromFile(ISimpleTitle page)
		{
			var pageName = page.PageName[FurnishingPrefix.Length..];
			var extension = pageName.LastIndexOf('.');
			if (extension >= 0)
			{
				pageName = pageName.Substring(0, extension);
			}

			return new Title(page.Namespace.Site[UespNamespaces.Online], pageName);
		}
		#endregion
	}
}
