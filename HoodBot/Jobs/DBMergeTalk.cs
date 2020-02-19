namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;

	public class DBMergeTalk : PageMoverJob
	{
		#region Fields
		private readonly TitleCollection deleted;
		private readonly TitleCollection ignored;
		#endregion

		#region Constructors

		[JobInfo("Merge Talk", "Dragonborn Merge")]
		public DBMergeTalk(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.ignored = new TitleCollection(site);
			this.deleted = new TitleCollection(site);
			this.FollowUpActions = FollowUpActions.EmitReport;
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.ignored.GetCategoryMembers("DBMerge-No Bot");
			this.deleted.GetCategoryMembers("Marked for Deletion");
			this.deleted.FilterToNamespaces(UespNamespaces.Dragonborn, UespNamespaces.DragonbornTalk, UespNamespaces.Skyrim, UespNamespaces.SkyrimTalk);
			base.BeforeLogging();
		}

		protected override void PopulateReplacements()
		{
			var dbPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties);
			dbPages.GetNamespace(UespNamespaces.Dragonborn, Filter.Any);
			dbPages.GetNamespace(UespNamespaces.DragonbornTalk, Filter.Any);
			foreach (var delPage in this.deleted)
			{
				dbPages.Remove(delPage);
			}

			var srTitles = new TitleCollection(this.Site);
			foreach (var dbPage in dbPages)
			{
				srTitles.Add(new Title(this.Site, dbPage.Namespace.IsTalkSpace ? UespNamespaces.SkyrimTalk : UespNamespaces.Skyrim, dbPage.PageName));
			}

			var srPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties);
			srPages.GetTitles(srTitles);

			foreach (var dbPage in dbPages)
			{
				var srPageName = "Skyrim:" + dbPage.PageName;
				if (dbPage.Namespace.IsTalkSpace)
				{
					if (dbPage.PageName.Contains("Archive", System.StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					srPageName += "/Dragonborn Archive";
				}

				if (!srPages.TryGetValue(srPageName, out var srPage))
				{
					srPage = new Page(this.Site, srPageName);
				}

				var replacement = new Replacement(dbPage, srPage);
				if (dbPage.IsRedirect || dbPage.IsDisambiguation)
				{
					replacement.Actions = ReplacementActions.Skip;
					replacement.Reason = "Source is a redirect or disambiguation.";
				}
				else if (srPage.Exists && (srPage.IsRedirect || srPage.IsDisambiguation))
				{
					replacement.Actions = ReplacementActions.Skip;
					replacement.Reason = "Destination is a redirect or disambiguation.";
				}

				this.Replacements.Add(replacement);
			}
		}
		#endregion
	}
}