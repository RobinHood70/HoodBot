namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public class DBMergeArticles : PageMoverJob
	{
		#region Constructors
		[JobInfo("Merge Pages", "Dragonborn Merge")]
		public DBMergeArticles(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			DeleteFiles();
			//// this.FollowUpActions = FollowUpActions.EmitReport;
			this.MoveOptions = MoveOptions.MoveTalkPage;
			this.RedirectOption = RedirectOption.Create;
			this.ReplaceSingleNode = this.FixNsBase;
		}
		#endregion

		#region Public Static Properties
		public static Dictionary<string, string> SpecialCases => new Dictionary<string, string>
		{
			["Benkum"] = "Benkum (Dragonborn)",
			["Eydis"] = "Eydis (Dragonborn)",
			["Items"] = "Dragonborn Items",
			["Letter"] = "Letter (Dragonborn)",
			["Liesl"] = "Liesl (Dragonborn)",
			["Mogrul"] = "Mogrul (Dragonborn)",
			["Nikulas"] = "Nikulas (Dragonborn)",
			["NPCs"] = "Dragonborn NPCs",
			["Places"] = "Dragonborn Places",
			["Quests"] = "Dragonborn Quests",
			["Torn Note"] = "Torn Note (Dragonborn)",
		};
		#endregion

		#region Public Static Methods
		public static void UpdateFileParameter(PageMoverJob job, int ns, NodeCollection value)
		{
			ThrowNull(job, nameof(job));
			ThrowNull(value, nameof(value));
			var split = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(value));
			var title = new Title(job.Site, ns, split.Value);
			if (job.Replacements.TryGetValue(title, out var replacement))
			{
				split.Value = replacement.To.PageName;
				value.Clear();
				value.AddText(split.ToString());
			}
		}
		#endregion

		#region Protected Override Methods
		protected override void FilterBacklinks(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			base.FilterBacklinks(backlinkTitles);
			backlinkTitles.Remove("User:HoodBot/Dragonborn Merge Actions");
		}

		protected override void PopulateReplacements()
		{
			static void RemoveTitleAndTalk(PageCollection dbPages, Title dbTitle)
			{
				dbPages.Remove(dbTitle.SubjectPage);
				if (dbTitle.TalkPage != null)
				{
					dbPages.Remove(dbTitle.TalkPage);
				}
			}

			var dbPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties);
			dbPages.GetNamespace(UespNamespaces.Dragonborn, Filter.Any);
			dbPages.GetNamespace(UespNamespaces.DragonbornTalk, Filter.Any);

			var ignored = new TitleCollection(this.Site);
			ignored.GetCategoryMembers("DBMerge-Redirects");
			ignored.RemoveNamespaces(UespNamespaces.Skyrim);
			ignored.Remove("Dragonborn:Places"); // Transcludes other pages with the redirect.
			ignored.GetCategoryMembers("DBMerge-No Bot");
			foreach (var title in ignored)
			{
				RemoveTitleAndTalk(dbPages, title);
			}

			ignored.Clear();
			ignored.GetCategoryMembers("DBMerge-Merged");
			foreach (var title in ignored)
			{
				var dbTitle = new Title(this.Site, UespNamespaces.Dragonborn, title.PageName);
				RemoveTitleAndTalk(dbPages, dbTitle);
			}

			var deleted = new TitleCollection(this.Site);
			deleted.GetCategoryMembers("Marked for Deletion");
			foreach (var title in deleted)
			{
				RemoveTitleAndTalk(dbPages, title);
			}

			var srTitles = new TitleCollection(this.Site);
			foreach (var dbPage in dbPages)
			{
				if (!SpecialCases.TryGetValue(dbPage.PageName, out var srPageName))
				{
					srPageName = dbPage.PageName;
				}

				var srTitle = new Title(this.Site, dbPage.Namespace.IsTalkSpace ? UespNamespaces.SkyrimTalk : UespNamespaces.Skyrim, srPageName);
				if (deleted.Contains(srTitle))
				{
					this.Warn($"* {srTitle.AsLink()} is proposed for deletion. Admin may want to delete before bot run.");
				}

				this.Replacements.Add(new Replacement(dbPage, srTitle));
			}

			var srPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties);
			srPages.GetTitles(srTitles);
			foreach (var srPage in srPages)
			{
				if (srPage.Exists)
				{
					var replacement = this.Replacements[srPage];
					replacement.Actions = ReplacementActions.Skip;
					replacement.Reason = "Skyrim page exists.";
				}
			}
		}
		#endregion

		#region Private Methods
		private void FixNsBase(Page page, IWikiNode node)
		{
			if (node is TemplateNode template && template.GetTitleValue()?.ToLowerInvariant() != "map link" && template.FindParameterLinked("ns_base") is LinkedListNode<IWikiNode> paramListNode && paramListNode.Value is ParameterNode nsBase)
			{
				var value = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(nsBase.Value));
				if (this.Site.Namespaces[UespNamespaces.Dragonborn].Contains(value.Value))
				{
					if (page.Namespace.SubjectSpaceId == UespNamespaces.Skyrim)
					{
						template.Parameters.Remove(nsBase);
					}
					else
					{
						nsBase.Value.Clear();
						nsBase.Value.AddText(value.Value == "DB" ? "SR" : "Skyrim");
					}
				}
			}

			/*
			if (node is IBacklinkNode backlinkNode)
			{
				var titleText = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(backlinkNode.Title));
				var title = new TitleParts(this.Site, titleText.Value);
				if (title.NamespaceId == UespNamespaces.Dragonborn || title.NamespaceId == UespNamespaces.DragonbornTalk)
				{
					title.NamespaceId = title.Namespace.IsTalkSpace ? UespNamespaces.SkyrimTalk : UespNamespaces.Skyrim;
					titleText.Value = title.ToString();
					backlinkNode.Title.Clear();
					backlinkNode.Title.AddText(titleText.Value);
				}
			}
			*/
		}
		#endregion
	}
}
