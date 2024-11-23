namespace RobinHood70.HoodBot.Jobs;

using System;
using System.Collections.Generic;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.Robby.Parser;

internal sealed class SFGalaxy_Old : CreateOrUpdateJob<CsvRow>
{
	#region Fields
	private readonly Dictionary<string, string> stars = new(StringComparer.Ordinal);
	#endregion

	#region Constructors
	[JobInfo("Galaxy", "Starfield")]
	public SFGalaxy_Old(JobManager jobManager)
		: base(jobManager)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		this.NewPageText = this.GetNewPageText;
	}
	#endregion

	#region Protected Override Properties
	protected override string? Disambiguator => "planet";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Create planet page";

	protected override bool IsValid(SiteParser parser, CsvRow data) => parser.FindSiteTemplate("Planet Infobox") is not null;

	protected override IDictionary<Title, CsvRow> LoadItems()
	{
		var csv = new CsvFile(GameInfo.Starfield.ModFolder + "stars.csv")
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		csv.Load();
		foreach (var row in csv)
		{
			this.stars.Add(row["FormID"], row["Name"]);
		}

		var items = new Dictionary<Title, CsvRow>();
		csv = new CsvFile(GameInfo.Starfield.ModFolder + "galaxy.csv")
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		csv.Load();
		foreach (var item in csv)
		{
			var name = "Starfield:" + item["Name"];
			items.Add(TitleFactory.FromUnvalidated(this.Site, name), item);
		}

		return items;
	}
	#endregion

	#region Private Methods
	private string GetNewPageText(Title title, CsvRow item)
	{
		var starName = this.stars[item["Star ID"]];
		var starType = item["Type"]
			.Replace("G.", "Giant", StringComparison.Ordinal)
			.Replace("Aster.", "Asteroid", StringComparison.Ordinal);
		var magnetosphere = item["Mag. Field"];
		if (string.IsNullOrWhiteSpace(magnetosphere))
		{
			magnetosphere = "Unknown";
		}

		return "{{Planet Infobox\n" +
		"|image=\n" +
		$"|system={starName}\n" +
		$"|type={starType}\n" +
		$"|gravity={item["Gravity"]}\n" +
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