namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Models;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon; using RobinHood70.CommonCode;

	public class DBMergeIdentifyConflicts : EditJob
	{
		[JobInfo("Identify Conflicts", "Dragonborn Merge")]
		public DBMergeIdentifyConflicts(JobManager jobManager)
			: base(jobManager)
		{
			this.Logger = null;
			this.Results = new PageResultHandler(TitleFactory.FromName(this.Site, "User:HoodBot/Dragonborn Merge Actions"))
			{
				Description = "Update List"
			};
		}

		protected override void Main()
		{
			var dbPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties);
			dbPages.GetNamespace(UespNamespaces.Dragonborn, Filter.Any);

			var srPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties);
			srPages.GetNamespace(UespNamespaces.Skyrim, Filter.Any);

			var ignored = new TitleCollection(this.Site);
			ignored.GetCategoryMembers("DBMerge-No Bot");

			var deleted = this.GetProposedDeletions();

			dbPages.Sort();
			this.WriteLine("Any page not mentioned below will be moved normally.");
			this.WriteLine();
			this.WriteLine("{| class=\"wikitable sortable\"");
			this.WriteLine("|-");
			this.WriteLine("! Page !! Action");
			foreach (var dbPage in dbPages)
			{
				string? action = null;
				if (deleted.Contains(dbPage))
				{
					action = "Skip: proposed for deletion.";
				}
				else if (srPages.TryGetValue("Skyrim:" + dbPage.PageName, out var srPage))
				{
					action =
						deleted.Contains(srPage) ? $"Skip: [[{srPage.FullPageName}]] is proposed for deletion. Admin may want to delete before bot run." :
						dbPage.IsRedirect || dbPage.IsDisambiguation ? $"Skip: redirect or disambiguation." :
						srPage.IsRedirect || srPage.IsDisambiguation ? $"Skip: [[{srPage.FullPageName}]] is a redirect or disambiguation." :
						$"Merge into [[{srPage.FullPageName}]]";
				}

				if (action != null)
				{
					this.WriteLine("|-");
					this.WriteLine($"| [[{dbPage.FullPageName}|{dbPage.PageName}]] || {action}");
				}
			}

			var dbFiles = new TitleCollection(this.Site);
			dbFiles.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "DB-");

			var srFiles = new TitleCollection(this.Site);
			srFiles.GetNamespace(MediaWikiNamespaces.File, Filter.Exclude, "SR-");

			foreach (var dbFile in dbFiles)
			{
				var baseFileName = dbFile.PageName.Substring(3);
				if (srFiles.TryGetValue("File:SR-" + baseFileName, out var srFile))
				{
					this.WriteLine("|-");
					this.WriteLine($"| [[:{dbFile.FullPageName}|{dbFile.PageName}]] || Skip: [[:{srFile.FullPageName}|{srFile.PageName}]] already exists.");
				}
			}

			this.WriteLine("|}");
			this.Results?.Save();
		}
	}
}
