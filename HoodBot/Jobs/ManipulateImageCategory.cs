namespace RobinHood70.HoodBot.Jobs;

using RobinHood70.HoodBot.Jobs.Design;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon;

internal sealed class ManipulateImageCategory : WikiJob
{
	[JobInfo("Manipulate Image Category")]
	public ManipulateImageCategory(JobManager jobManager)
		: base(jobManager, JobType.ReadOnly)
	{
		var title = TitleFactory.FromUnvalidated(this.Site, "User:Jeancey/Kah");
		this.SetTemporaryResultHandler(new PageResultHandler(title, false));
	}

	protected override void Main()
	{
		// TODO: Switch to loading by TitleCollection, then save TitleCollection so reloads can be much faster.
		// Investigate why original attempt at this produced a recurring load that never completed.
		PageCollection list = new(this.Site, PageModules.FileInfo);
		list.SetLimitations(LimitationType.OnlyAllow, MediaWikiNamespaces.File);
		list.GetCategoryMembers("Online-Icons", true);
		list.Sort();
		foreach (var page in list)
		{
			var fileInfo = (FilePageModule)page.Custom[FilePageModule.PropertyName];
			if (fileInfo.LatestFileRevision is FileRevision fileRevision &&
				fileRevision.Height < 64 &&
				fileRevision.Width < 64)
			{
				this.WriteLine($"* {SiteLink.ToText(page.Title, LinkFormat.LabelName)} ({fileRevision.Width}x{fileRevision.Height})");
			}
		}
	}
}