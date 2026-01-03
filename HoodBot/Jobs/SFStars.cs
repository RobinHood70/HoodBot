namespace RobinHood70.HoodBot.Jobs;

using System.IO;
using System.Text;
using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;

internal sealed class SFStars : CreateOrUpdateJob<CsvRow>
{
	#region Constructors
	[JobInfo("Stars", "Starfield")]
	public SFStars(JobManager jobManager)
		: base(jobManager)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
	}
	#endregion

	#region Protected Override Properties
	protected override string? GetDisambiguator(CsvRow item) => "star";
	#endregion

	#region Protected Override Methods
	protected override string GetEditSummary(Page page) => "Create star";

	protected override TitleDictionary<CsvRow> GetExistingItems() => [];

	protected override void GetExternalData()
	{
		// NOTE: This was a hasty conversion to the new format that just stuffs everything in GetExternalData(). If used again in the future, it should probably be separated into its proper GetExternal/GetExisting/GetNew components.
		var csvName = GameInfo.Starfield.ModFolder + "stars.csv";
		if (!File.Exists(csvName))
		{
			return;
		}

		var csv = new CsvFile(csvName)
		{
			Encoding = Encoding.GetEncoding(1252)
		};

		csv.Load();
		foreach (var row in csv)
		{
			var title = TitleFactory.FromUnvalidated(this.Site, "Starfield:" + row["Name"] + " System");
			this.Items.Add(title, row);
		}
	}

	protected override TitleDictionary<CsvRow> GetNewItems() => [];
	#endregion

	#region Private Static Methods
	protected override string GetNewPageText(Title title, CsvRow item) =>
		"{{Trail|Places}}\n" +
		"{{System Infobox\n" +
		$"|eid={item["FormID"]}\n" +
		$"|name={item["Name"]}\n" +
		$"|class={item["Spectral"]}\n" +
		$"|id={item["Gliese"]}\n" +
		$"|temp={item["Temperature"]}\n" +
		$"|magnitude={item["AbsMag"]}\n" +
		"|image=\n" +
		"|level=\n" +
		"|mass=\n" +
		"|moon=\n" +
		"|planet=\n" +
		"|radius=\n" +
		"}}\n\n" +
		"{{NewLine}}\n" +
		"{{System Table}}";
	#endregion
}