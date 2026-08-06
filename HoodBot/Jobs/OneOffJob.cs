namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Parser;
using RobinHood70.WikiCommon;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	protected override void Main()
	{
		var pages = new PageCollection(this.Site);
		pages.GetBacklinks("Template:NPC Summary", BacklinksTypes.EmbeddedIn, true, Filter.Exclude, UespNamespaces.ProjectTamriel);
		pages.Sort();
		foreach (var page in pages)
		{
			if (!page.Title.PageName.StartsWith("Skyrim/", System.StringComparison.InvariantCultureIgnoreCase))
			{
				continue;
			}

			var parser = new SiteParser(page);
			var found = false;
			foreach (var template in parser.TemplateNodes)
			{
				if (template.GetTitle(this.Site).PageNameEquals("MW Racial Note"))
				{
					found = true;
					break;
				}
			}

			if (!found)
			{
				this.WriteLine($"* Missing MW Racial Note template on [[{page.Title}]]");
			}
		}
	}
}