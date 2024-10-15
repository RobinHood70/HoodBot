namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	internal sealed class SFPlanets : CreateOrUpdateJob<CsvRow>
	{
		public SFPlanets(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override string? Disambiguator { get; }

		protected override string GetEditSummary(Page page)
		{
			return string.Empty;
		}

		protected override bool IsValid(SiteParser parser, CsvRow item)
		{
			return false;
		}

		protected override IDictionary<Title, CsvRow> LoadItems()
		{
			return new Dictionary<Title, CsvRow>();
		}
	}
}
