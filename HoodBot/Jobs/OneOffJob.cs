namespace RobinHood70.HoodBot.Jobs;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using RobinHood70.CommonCode;
using RobinHood70.Robby.Design;
using RobinHood70.WallE.Base;
using RobinHood70.WikiCommon;

[method: JobInfo("One-Off Job")]
internal sealed class OneOffJob(JobManager jobManager) : WikiJob(jobManager, JobType.ReadOnly)
{
	#region Protected Override Methods
	protected override void Main()
	{
		var lines = new List<string>();
		for (var letter = 'A'; letter <= 'Z'; letter++)
		{
			var title = TitleFactory.FromUnvalidated(this.Site, "Lore:Bestiary " + letter);
			var input = new BacklinksInput(title.FullPageName(), BacklinksTypes.Backlinks) { FilterRedirects = Filter.Any, Redirect = true };
			var result = this.Site.AbstractionLayer.Backlinks(input);
			var items = result
				.Where(bl => bl.Namespace != MediaWikiNamespaces.User)
				.OrderByDescending(bl => bl.Redirects.Count);
			foreach (var item in items)
			{
				lines.Add($"{title} => {item.Title}: {item.Redirects.Count}");
			}
		}

		File.WriteAllLines(@"D:\Bestiary Most Incoming Redirects.txt", lines);
	}
	#endregion
}