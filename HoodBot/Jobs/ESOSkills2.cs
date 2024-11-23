namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;

[method: JobInfo("Get Skills 2")]
internal sealed class EsoSkills2(JobManager jobManager) : CreateOrUpdateJob<Skill>(jobManager)
{
	#region Protected Override Properties
	protected override string? Disambiguator => "skill";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Create/update skill";

	protected override bool IsValid(SiteParser parser, Skill item) => parser.FindSiteTemplate(Skill.SummaryTemplate) is not null;

	protected override IDictionary<Title, Skill> LoadItems()
	{
		var retval = new Dictionary<Title, Skill>();

		OpenBrowser().GetAwaiter().GetResult();

		return retval;
	}
	#endregion

	#region Private Methods
	private static async Task OpenBrowser()
	{
		using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
		var chromium = playwright.Chromium;
		var browser = await chromium.LaunchAsync(new() { Headless = false }).ConfigureAwait(false);
		var page = await browser.NewPageAsync().ConfigureAwait(false);
		await page.GotoAsync("https://www.bing.com").ConfigureAwait(false);
		await browser.CloseAsync().ConfigureAwait(false);
	}
	#endregion
}