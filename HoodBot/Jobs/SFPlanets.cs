namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;

	internal sealed class SFPlanets : CreateOrUpdateJob<CsvRow>
	{
		#region Constructors
		[JobInfo("SF Planets")]
		public SFPlanets(JobManager jobManager)
			: base(jobManager)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		#endregion

		#region Protected Override Properties
		protected override string? Disambiguator => "planet";

		protected override string EditSummary => "Create planet page";
		#endregion

		#region Protected Override Methods
		protected override bool IsValid(ContextualParser parser, CsvRow data) => parser.FindSiteTemplate("Planet Infobox") is not null;

		protected override IDictionary<Title, CsvRow> LoadItems()
		{
			var items = new Dictionary<Title, CsvRow>();
			var csv = new CsvFile() { Encoding = Encoding.GetEncoding(1252) };
			csv.Load(LocalConfig.BotDataSubPath("Starfield/Planets.csv"), true);
			foreach (var item in csv)
			{
				var name = "Starfield:" + item["Name"];
				items.Add(TitleFactory.FromUnvalidated(this.Site, name), item);
			}

			return items;
		}

		protected override string NewPageText(Title title, CsvRow item)
		{
			var starName = item["StarName"];
			var planetType = item["Type"]
				.Replace("G.", "Giant", StringComparison.Ordinal)
				.Replace("Aster.", "Asteroid", StringComparison.Ordinal);
			var gravityText = double.TryParse(item["Gravity"], CultureInfo.CurrentCulture, out var grav) ? grav.ToStringInvariant() : string.Empty;
			var magnetosphere = item["MagField"];
			if (string.IsNullOrWhiteSpace(magnetosphere))
			{
				magnetosphere = "Unknown";
			}

			return "{{Planet Infobox\n" +
			"|image=\n" +
			$"|system={starName}\n" +
			$"|type={planetType}\n" +
			$"|gravity={gravityText}\n" +
			"|temp=\n" +
			"|atmosphere=\n" +
			$"|magnetosphere={magnetosphere}\n" +
			"|fauna=\n" +
			"|flora=\n" +
			"|water=\n" +
			"|resource=\n" +
			"|trait=\n" +
			"}}\n\n{{Stub|Planet}}";
		}
		#endregion
	}
}