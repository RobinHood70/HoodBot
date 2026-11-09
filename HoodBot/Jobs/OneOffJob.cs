namespace RobinHood70.HoodBot.Jobs;

using System;
using RobinHood70.CommonCode;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon;
using RobinHood70.WikiCommon.Parser;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Protected Override Methods
	protected override void Main()
	{
		var pages = new PageCollection(this.Site);
		pages.GetBacklinks("Template:Non-Relevant NPC", BacklinksTypes.EmbeddedIn, false, Filter.Only);
		pages.Sort();
		foreach (var page in pages)
		{
			var target = page.GetRedirectLink() ?? throw new InvalidOperationException($"Redirect {page.Title} has no target.");
			var fullTitle = TitleFactory.FromTitleNode(page.Site, target);
			if (string.IsNullOrWhiteSpace(fullTitle.Fragment))
			{
				this.WriteLine($"* [{page.CanonicalPath}?action=edit {page.Title}] redirects to {target.ToRaw()}");
			}
		}
	}
	#endregion
}