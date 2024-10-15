namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	[method: JobInfo("Fix Zenimage licences", "Maintenance")]
	public class FixZenimageLicences(JobManager jobManager) : TemplateJob(jobManager)
	{
		#region Protected Override Properties
		protected override string TemplateName { get; } = "Uespimage";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Fix licencing";

		protected override void LoadPages()
		{
			HashSet<string> prefixes = new(StringComparer.OrdinalIgnoreCase) { "book", "card", "concept", "crown store", "icon", "load", "map", "mapicon", "prerelease", "render", "tribute" };
			TitleCollection uespImageTitles = new(this.Site);
			uespImageTitles.GetBacklinks("Template:Uespimage", BacklinksTypes.EmbeddedIn);
			TitleCollection goodTitles = new(this.Site);
			foreach (var title in uespImageTitles)
			{
				var titleSplit = title.PageName.Split('-');
				if (titleSplit.Length > 2 &&
					string.Equals(titleSplit[0], "ON", StringComparison.Ordinal) &&
					prefixes.Contains(titleSplit[1]))
				{
					goodTitles.Add(title);
				}
			}

			this.Pages.GetTitles(goodTitles);
		}

		protected override void ParseTemplate(SiteTemplateNode template, SiteParser parser)
		{
			template.TitleNodes.Clear();
			template.TitleNodes.AddText("Zenimage");
			template.Parameters.Clear();
		}
		#endregion
	}
}