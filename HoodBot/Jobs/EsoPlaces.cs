namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class EsoPlaces : EditJob
	{
		[JobInfo("Create Missing Places", "ESO")]
		public EsoPlaces(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override void BeforeLogging()
		{
			var lookup = new Dictionary<string, int>(StringComparer.Ordinal);
			var titles = new TitleCollection(this.Site);
			foreach (var (name, data) in EsoGeneral.GetZonesFromDatabase())
			{
				lookup.TryAdd(name, data);
				titles.Add(UespNamespaces.Online, name);
			}

			var pages = PageCollection.Unlimited(this.Site);
			pages.GetTitles(titles);
			foreach (var page in pages)
			{
				Debug.WriteLine($"* [[{page.FullPageName}]]");
				var parsedPage = new ContextualParser(page);
				var template = parsedPage.FindTemplate("Online Page Summary");
				if (page.Exists && template != null)
				{
					var mapType = lookup[page.PageName];
					Debug.WriteLine($"{mapType.ToStringInvariant()}, {template.Find("type")?.Value}, {template.Find("zone")?.Value}, {template.Find("zoneName")?.Value}, {template.Find("settlement")?.Value}");
				}
			}
		}

		protected override void Main()
		{
		}
	}
}
