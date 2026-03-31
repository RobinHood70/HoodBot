namespace RobinHood70.HoodBot.Jobs;

using System.Diagnostics;
using RobinHood70.Robby;
using RobinHood70.Robby.Design;
using RobinHood70.WikiCommon;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.Write)
{
	#region Protected Override Methods
	protected override void Main()
	{
		string[] cats = [
			"Daggerfall-TEXTURE.000 (Solid Colors A)",
			"Daggerfall-TEXTURE.003",
			"Daggerfall-TEXTURE.004",
			"Daggerfall-TEXTURE.007",
			"Daggerfall-TEXTURE.009",
			"Daggerfall-TEXTURE.010",
			"Daggerfall-TEXTURE.011",
			"Daggerfall-TEXTURE.012",
			"Daggerfall-TEXTURE.013-0.30",
			"Daggerfall-TEXTURE.031",
			"Daggerfall-TEXTURE.033-0.47",
			"Daggerfall-TEXTURE.051",
			"Daggerfall-TEXTURE.093"
		];

		var pages = new PageCollection(this.Site, PageModules.Backlinks);
		foreach (var cat in cats)
		{
			pages.GetCategoryMembers(cat, CategoryMemberTypes.File, false);
		}

		this.Progress = pages.Count;
		foreach (var page in pages)
		{
			if (page.Backlinks.Count == 0)
			{
				Debug.WriteLine(page.Title);
				page.Title.Delete("Author request");
			}

			this.Progress++;
		}
	}
	#endregion
}