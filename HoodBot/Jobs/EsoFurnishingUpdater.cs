namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class EsoFurnishingUpdater : TemplateJob
	{
		#region Fields
		private readonly Dictionary<Title, IEnumerable<string>> imageCategories = new(SimpleTitleEqualityComparer.Instance);
		#endregion

		#region Constructors
		[JobInfo("ESO Furnishing Updater", "|ESO")]
		public EsoFurnishingUpdater(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string EditSummary { get; } = "Update info from ESO database";

		protected override string TemplateName { get; } = "Online Furnishing Summary";
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var titles = new TitleCollection(this.Site, this.imageCategories.Keys);
			PageCollection pages = PageCollection.Unlimited(this.Site, PageModules.Default, false);
			var saveInfo = new SaveInfo("Update categories", true);
			pages.PageLoaded += this.ImagePageLoaded;
			pages.GetTitles(titles);
			pages.PageLoaded -= this.ImagePageLoaded;
			this.SavePages(pages, "Saving image categories", saveInfo, this.ImagePageLoaded);

		}

		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parsedPage)
		{
			var isCollectible = template.TrueOrFalse("collectible");
			var itemName = parsedPage.Context.PageName
				.Replace("item-", string.Empty, StringComparison.Ordinal)
				.Replace("furnishing-", string.Empty, StringComparison.Ordinal)
				.Replace(" (furnishing)", string.Empty, StringComparison.Ordinal)
				.Replace(".jpg", string.Empty, StringComparison.Ordinal);
			var defaultFile = $"ON-{(isCollectible ? string.Empty : "item-")}furnishing-{itemName}.jpg";

			var image = template.ValueOrDefault("image", defaultFile).Trim();
			List<string> imgCats = new();
			imgCats.Add("Category:Online-Furnishings");
			var baseCat = "Category:Online-Furnishing Images-";
			var cat = AddCategory(imgCats, template, "cat", $"{baseCat}$1");
			AddCategory(imgCats, template, "subcat", $"{baseCat}{cat}-$1");
			AddCategory(imgCats, template, "size", $"{baseCat}$1");
			AddCategory(imgCats, template, "style", $"{baseCat}$1");
			AddCategory(imgCats, template, "achievement", $"{baseCat}Achievement");
			AddCategory(imgCats, template, "antiquity", $"{baseCat}Antiquities");
			AddCategory(imgCats, template, "tags", $"{baseCat}By Behavior");
			AddCategory(imgCats, template, "craft", $"{baseCat}By Profession");
			AddCategory(imgCats, template, "collectible", $"{baseCat}Collectible");
			AddCategory(imgCats, template, "luxury", $"{baseCat}Luxury");
			AddCategory(imgCats, template, "master", $"{baseCat}Masterworks");
			AddCategory(imgCats, template, "source", $"{baseCat}Sources");

			Title imageTitle = TitleFactory.FromName(this.Site, MediaWikiNamespaces.File, image).ToTitle();
			this.imageCategories.Add(imageTitle, imgCats);
		}
		#endregion

		#region Private Static Methods
		private static string? AddCategory(ICollection<string> imgCats, SiteTemplateNode template, string paramName, string pattern)
		{
			if (template.Find(paramName) is IParameterNode cat)
			{
				var catName = cat.Value.ToRaw().Trim();
				var fullName = pattern.Replace("$1", catName, StringComparison.Ordinal);
				imgCats.Add(fullName);
				return catName;
			}

			return null;
		}
		#endregion

		#region Private Methods

		private void ImagePageLoaded(object sender, Page page)
		{
			if (page.Exists)
			{
				IEnumerable<string> catsToAdd = this.imageCategories[page];
				ContextualParser parser = new(page);
				foreach (var cat in catsToAdd)
				{
					parser.AddCategory(cat, true);
				}

				page.Text = parser.ToRaw();
			}
			else
			{
				Debug.WriteLine($"Missing page: {page.FullPageName}");
			}
		}
		#endregion
	}
}
