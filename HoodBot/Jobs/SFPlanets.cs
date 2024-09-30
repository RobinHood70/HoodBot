namespace RobinHood70.HoodBot.Jobs
{
	using System;
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
			throw new NotImplementedException();
		}

		protected override bool IsValid(ContextualParser parser, CsvRow item)
		{
			throw new NotImplementedException();
		}

		protected override IDictionary<Title, CsvRow> LoadItems()
		{
			throw new NotImplementedException();
		}

		protected override string NewPageText(Title title, CsvRow item)
		{
			throw new NotImplementedException();
		}
	}
}
