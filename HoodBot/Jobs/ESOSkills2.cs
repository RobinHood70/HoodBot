namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Microsoft.Playwright;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	internal sealed class EsoSkills2 : CreateOrUpdateJob<EsoSkills2.Skill>
	{
		#region Constructors
		[JobInfo("Get Skills 2")]
		public EsoSkills2(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "skill";

		protected override string EditSummary => "Create/update skill";
		#endregion

		#region Protected Override Methods
		protected override bool IsValid(ContextualParser parser, Skill item) => throw new NotImplementedException();

		protected override IDictionary<Title, Skill> LoadItems()
		{
			var retval = new Dictionary<Title, Skill>();

			this.OpenBrowser().GetAwaiter().GetResult();

			return retval;
		}

		protected override string NewPageText(Title title, Skill item) => throw new NotImplementedException();
		#endregion

		#region Private Methods
		private async Task OpenBrowser()
		{
			using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
			var chromium = playwright.Chromium;
			var browser = await chromium.LaunchAsync(new() { Headless = false }).ConfigureAwait(false);
			var page = await browser.NewPageAsync().ConfigureAwait(false);
			await page.GotoAsync("https://www.bing.com").ConfigureAwait(false);
			await browser.CloseAsync().ConfigureAwait(false);
		}
		#endregion

		#region Internal Records
		internal sealed record Skill(string SkillId);
		#endregion

	}
}