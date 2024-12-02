namespace RobinHood70.HoodBot.Jobs;

using System.Diagnostics;
using RobinHood70.HoodBot.Jobs.JobModels;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon;

[method: JobInfo("Find All ESO Icons", "ESO")]
internal sealed class FindAllEsoIcons(JobManager jobManager) : EditJob(jobManager)
{
	protected override string GetEditSummary(Page page) => "Add Online File";

	protected override void LoadPages()
	{
		var checksums = EsoSpace.GetIconChecksums();
		this.Pages.Modules = PageModules.Info | PageModules.FileInfo;
		this.Pages.GetNamespace(MediaWikiNamespaces.File, CommonCode.Filter.Exclude, "ON-icon-achievement-R");
		foreach (var page in this.Pages)
		{
			if (page is FilePage file)
			{
				foreach (var fileRev in file.FileRevisions)
				{
					if (fileRev.Sha1 is not null && checksums.TryGetValue(fileRev.Sha1, out var fileList))
					{
						Debug.WriteLine(file.Title.FullPageName() + " matched " + string.Join(", ", fileList));
					}
				}
			}
		}
	}

	protected override void PageLoaded(Page page)
	{
	}
}