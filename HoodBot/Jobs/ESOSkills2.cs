﻿namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Threading.Tasks;
	using Microsoft.Playwright;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	[method: JobInfo("Get Skills 2")]
	[SuppressMessage("Style", "IDE0001:Name can be simplified", Justification = "False hit.")]
	internal sealed class EsoSkills2(JobManager jobManager) : CreateOrUpdateJob<EsoSkills2.Skill>(jobManager)
	{
		#region Protected Override Properties
		protected override string? Disambiguator => "skill";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create/update skill";

		protected override bool IsValid(SiteParser parser, Skill item) => throw new NotImplementedException();

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

		#region Internal Records
		internal sealed record Skill(string SkillId);
		#endregion
	}
}