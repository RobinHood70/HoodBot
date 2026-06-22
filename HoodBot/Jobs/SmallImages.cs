namespace RobinHood70.HoodBot.Jobs;

using System.Globalization;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;

[method: JobInfo("Find Small Images")]
internal sealed class SmallImages(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Protected Override Methods
	protected override void Main()
	{
		PageCollection files = new(this.Site, PageModules.FileInfo);
		files.GetCategoryMembers("Category:Legends-Art", true);
		files.Sort();
		foreach (var page in files)
		{
			var fileInfo = (FilePageModule)page.Custom[FilePageModule.PropertyName];
			if (fileInfo.LatestFileRevision is FileRevision rev && rev.Height <= 512 && rev.Width <= 512)
			{
				this.WriteLine(string.Create(
					CultureInfo.InvariantCulture,
					$"* {SiteLink.ToText(page.Title, LinkFormat.LabelName)}: {rev.Width} × {rev.Height}"));
			}
		}
	}
	#endregion
}