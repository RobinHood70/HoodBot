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
		[JobInfo("Create Missing Places", "ESO")] // ESO Update if this ever becomes reliable
		public EsoPlaces(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override void BeforeLogging()
		{
			Dictionary<string, int> lookup = new(StringComparer.Ordinal);
			TitleCollection titles = new(this.Site);
			foreach (var (name, data) in EsoLog.GetZones())
			{
				lookup.TryAdd(name, data);
				titles.Add(UespNamespaces.Online, name);
			}

			PageCollection pages = PageCollection.Unlimited(this.Site);
			pages.GetTitles(titles);
			foreach (var page in pages)
			{
				Debug.WriteLine($"* [[{page.FullPageName}]]");
				ContextualParser parsedPage = new(page);
				var template = parsedPage.FindSiteTemplate("Online Page Summary");
				if (page.Exists && template != null)
				{
					var mapType = lookup[page.PageName];
					Debug.WriteLine($"{mapType.ToStringInvariant()}, {template.GetValue("type")}, {template.GetValue("zone")}, {template.GetValue("zoneName")}, {template.GetValue("settlement")}");
				}
			}
		}

		protected override void Main()
		{
		}
	}
}
