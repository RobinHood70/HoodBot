namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Parse Job")]
internal sealed class OneOffParseJob(JobManager jobManager) : ParsedPageJob(jobManager)
{
	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Renommer les modèles pour plus d'uniformité";

	protected override void LoadPages()
	{
		var imageList = new TitleCollection(this.Site);
		var pages = new PageCollection(this.Site);
		pages.GetCategoryMembers("Online-Furnishings-Library-Readable Literature", CategoryMemberTypes.Page, false);
		foreach (var page in pages)
		{
			var parser = new SiteParser(page);
			if (parser.FindTemplate("Online Furnishing Summary") is not TemplateNode template)
			{
				this.WriteLine($"* [[{page.Title}]] does not have the template.");
				continue;
			}

			if (template.GetValue("image") is not string image || image.Length == 0)
			{
				var pageName = page.Title.LabelName().Replace(':', ',');
				image = $"ON-furnishing-{pageName}.jpg";
			}

			imageList.TryAdd("File:" + image);
		}

		this.Pages.GetTitles(imageList);
	}

	protected override void ParseText(SiteParser parser)
	{
		if (parser.Page.Exists)
		{
			parser.AddCategoryAfter(this.Site, "Online-Furnishing Images-Library-Readable Literature", "Online-Furnishing Images-Library-Literature", true);
		}
	}
	#endregion
}