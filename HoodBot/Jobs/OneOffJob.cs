namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.CommonCode;
using RobinHood70.HoodBot.Uesp;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	protected override void Main()
	{
		var pages = PageCollection.Unlimited(this.Site, PageModules.Backlinks, true);
		pages.GetCategoryMembers("Online-Furnishing Images", true);
		pages.Sort();
		this.StatusWriteLine(pages.Count.ToStringInvariant() + " pages");
		foreach (var page in pages)
		{
			foreach (var link in page.Backlinks)
			{
				if (link.Key.Namespace == UespNamespaces.Lore)
				{
					this.WriteLine($"* [[:{page.Title}]] => [[{link.Key}]]");
				}
			}
		}

		this.Results?.Save();
	}
}