namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.Robby;
using RobinHood70.WikiCommon;

[method: JobInfo("Purge List")]
internal sealed class PurgeSpecialList(JobManager jobManager) : WikiJob(jobManager, JobType.UnloggedWrite)
{
	protected override void Main()
	{
		var titles = new TitleCollection(this.Site);
		titles.GetQueryPage("Uncategorizedimages");
		_ = PageCollection.Purge(this.Site, titles, PurgeMethod.LinkUpdate, 50);
	}
}