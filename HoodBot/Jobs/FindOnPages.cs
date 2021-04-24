namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class FindOnPages : WikiJob
	{
		#region Constructors
		[JobInfo("Find On Pages")]
		public FindOnPages(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var pages = new PageCollection(this.Site);
			pages.GetBacklinks("Template:Online Ingredient Summary", BacklinksTypes.EmbeddedIn);
			if (pages.Count == 0)
			{
				Debug.WriteLine("No pages returned!");
			}
			else
			{
				var found = false;
				foreach (var page in pages)
				{
					if (page.Text.Contains(":image", StringComparison.OrdinalIgnoreCase) ||
						page.Text.Contains(":width", StringComparison.OrdinalIgnoreCase))
					{
						Debug.WriteLine(page.FullPageName);
						found = true;
					}
				}

				if (!found)
				{
					Debug.WriteLine("Nothing found!");
				}
			}
		}
		#endregion
	}
}