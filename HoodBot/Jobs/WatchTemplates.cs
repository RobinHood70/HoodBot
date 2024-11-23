namespace RobinHood70.HoodBot.Jobs;

using System.Diagnostics;
using RobinHood70.WikiCommon;

[method: JobInfo("Watch Template Space")]
internal sealed class WatchTemplates(JobManager jobManager) : WikiJob(jobManager, JobType.UnloggedWrite)
{
	#region Protected Override Methods
	protected override void Main()
	{
		var pages = this.Site.Watch(MediaWikiNamespaces.Template);
		foreach (var page in pages.Value)
		{
			Debug.WriteLine(page.Title.PageName);
		}
	}
	#endregion
}