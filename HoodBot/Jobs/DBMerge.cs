namespace RobinHood70.HoodBot.Jobs
{
	using System.IO;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public class DBMerge : PageMoverJob
	{
		#region Fields
		private readonly TitleCollection deleted;
		private readonly TitleCollection ignored;
		#endregion

		#region Constructors

		[JobInfo("Merge Pages", "Dragonborn Merge")]
		public DBMerge(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.ignored = new TitleCollection(site);
			this.deleted = new TitleCollection(site);
			this.FollowUpActions = FollowUpActions.EmitReport | FollowUpActions.FixLinks;
			File.Delete(BacklinksFile);
			File.Delete(ReplacementStatusFile);
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

		protected override void FilterSitePages(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			base.FilterSitePages(backlinkTitles);
			backlinkTitles.Remove("User:HoodBot/Dragonborn Merge Actions");
		}

		protected override void HandleConflict(Replacement replacement)
		{
			ThrowNull(replacement, nameof(replacement));
			if (this.deleted.Contains(replacement.To))
			{
				replacement.Action = ReplacementAction.Skip;
				replacement.ActionReason = $"[[{replacement.To.FullPageName}]] is proposed for deletion. Admin may want to delete before bot run.";
			}
			else
			{
				replacement.Action = ReplacementAction.CustomMove;
				replacement.ActionReason = "Merge page into DB section.";
			}
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
				srPageName = srPageName switch
				{
					"Skyrim:Items" => "Skyrim:Dragonborn Items",
					_ => srPageName
				};

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
					this.EditPages.Add(dbPage);
					replacement.Action = ReplacementAction.EditOnly;
					replacement.ActionReason = "Source is a redirect or disambiguation.";
				}
				else if (srPage.Exists && (srPage.IsRedirect || srPage.IsDisambiguation))
				{
					this.EditPages.Add(srPage);
					replacement.Action = ReplacementAction.EditOnly;
					replacement.ActionReason = "Destination is a redirect or disambiguation.";
				}

				this.Replacements.Add(replacement);
			}
		}
		#endregion
	}
}
