namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	[method: JobInfo("Orphaned Names")]
	internal sealed class OrphanedNames(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
	{
		#region Protected Override Methods
		protected override void Main()
		{
			var namesPages = new TitleCollection(
				this.Site,
				UespNamespaces.Lore,
				UespFunctions.LoreNames);
			var namePages = new PageCollection(this.Site, PageModules.Links);
			namePages.GetTitles(namesPages);
			var nameLinks = new TitleCollection(this.Site);
			foreach (var page in namePages)
			{
				foreach (var link in page.Links)
				{
					nameLinks.TryAdd(link);
				}
			}

			nameLinks.Remove(namesPages);
			var nameBackLinkPages = new PageCollection(this.Site, PageModules.Info | PageModules.Backlinks)
			{
				AllowRedirects = Filter.Exclude
			};

			nameBackLinkPages.GetTitles(nameLinks);
			foreach (var page in nameBackLinkPages)
			{
				var hasOtherLinks = false;
				foreach (var link in page.Backlinks)
				{
					if (link.Value == WikiCommon.BacklinksTypes.Backlinks && !namesPages.Contains(link.Key))
					{
						hasOtherLinks = true;
						break;
					}
				}

				if (!hasOtherLinks)
				{
					Debug.WriteLine(page.Title.FullPageName());
				}
			}
		}
		#endregion
	}
}