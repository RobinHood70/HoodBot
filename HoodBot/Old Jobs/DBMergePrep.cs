namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon.Parser;
	using RobinHood70.WikiCommon; using RobinHood70.CommonCode;

	public class DBMergePrep : ParsedPageJob
	{
		[JobInfo("Preparation", "Dragonborn Merge")]
		public DBMergePrep(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override string EditSummary => "Preparation for Dragonborn Merge";

		protected override void LoadPages()
		{
			var titles = this.GetAllTemplateTitles("Map Link", "Place Summary");
			titles.FilterToNamespaces(UespNamespaces.Dragonborn);
			this.Pages.GetTitles(titles);
		}

		protected override void ParseText(object sender, Page page, SiteParser parsedPage)
		{
			foreach (var template in parsedPage.NotNull(nameof(parsedPage)).FindAllRecursive<TemplateNode>())
			{
				var paramName = template.GetTitleValue() switch
				{
					"Map Link" => "ns_base",
					"Place Summary" => "ns_map",
					_ => null,
				};

				if (paramName != null)
				{
					var map = template.FindParameterLinked(paramName);
					if (map == null)
					{
						var mapName = template.ValueOf("mapname");
						if (mapName != "none")
						{
							template.AddParameter(paramName, "DB");
						}
					}
					else if (this.Site.Namespaces[UespNamespaces.Skyrim].Contains(WikiTextVisitor.Value(map.Value)))
					{
						template.Parameters.Remove(map);
					}
				}
			}
		}
	}
}
