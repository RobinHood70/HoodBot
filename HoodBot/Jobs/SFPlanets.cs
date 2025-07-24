namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;

internal sealed class SFPlanets(JobManager jobManager) : CreateOrUpdateJob<CsvRow>(jobManager)
{
	protected override string? Disambiguator { get; }

	protected override string GetEditSummary(Page page) => string.Empty;

	protected override bool IsValidPage(SiteParser parser, CsvRow item) => false;

	protected override IDictionary<Title, CsvRow> LoadItems() => new Dictionary<Title, CsvRow>();
}