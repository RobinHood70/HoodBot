namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;
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
		public FurnishingSplit([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
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
			ThrowNull(parsedPage.Title, nameof(parsedPage), nameof(parsedPage.Title));
			var issues = new List<string>();
			if (!parsedPage.Title.PageName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
			{
				issues.Add("does not end in .jpg");
			}

			var summary = parsedPage.FindFirst<TemplateNode>(node => node.GetTitleValue().ToLowerInvariant() == "furnishing summary");
			if (summary == null)
			{
				issues.Add("does not have a Furnishing Summary");
			}

			var title = PageFromFile(parsedPage.Title);
			if (this.existingPages!.Contains(title))
			{
				issues.Add($"page exists: {title.AsLink(true)}");
			}

			if (issues.Count > 0)
			{
				this.issues.Add($"* {parsedPage.Title.AsLink(false)}: {string.Join(", ", issues)}.");
			}
		}
		#endregion

		#region Private Methods
		private static ISimpleTitle PageFromFile(ISimpleTitle page)
		{
			var pageName = page.PageName.Substring(FurnishingPrefix.Length);
			var extension = pageName.LastIndexOf('.');
			if (extension >= 0)
			{
				pageName = pageName.Substring(0, extension);
			}

			return new Title(page.Namespace.Site, UespNamespaces.Online, pageName);
		}
		#endregion
	}
}
