namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Parser;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiClasses.Parser;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public class DBMergeMerge : PageMoverJob
	{
		#region Constructors

		[JobInfo("Merge Overlap", "Dragonborn Merge")]
		public DBMergeMerge(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			//// this.MoveAction = MoveAction.None;
			//// this.FollowUpActions = FollowUpActions.EmitReport;
			//// this.RedirectOption = RedirectOption.Create;
			this.FromPageModules = PageModules.Info | PageModules.Properties | PageModules.Revisions;
			this.ToPageModules = PageModules.Info | PageModules.Properties | PageModules.Revisions;
			this.CustomEdit = this.AddRedirCat;
			this.TemplateReplacements.Add("About", this.AboutHandler);
			this.TemplateReplacements.Add("DB", this.DbHandler);
		}
		#endregion

		#region Protected Override Methods
		protected override void FilterBacklinks(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			base.FilterBacklinks(backlinkTitles);
			backlinkTitles.Remove("User:HoodBot/Dragonborn Merge Actions");
			backlinkTitles.Remove("UESPWiki:Dragonborn Merge Project");

			var alreadyRedirected = new TitleCollection(this.Site);
			alreadyRedirected.GetCategoryMembers("DBMerge-Redirects");
			alreadyRedirected.RemoveNamespaces(UespNamespaces.Skyrim);
			foreach (var title in alreadyRedirected)
			{
				backlinkTitles.Remove(title);
			}
		}

		protected override void HandleConflict(Replacement replacement)
		{
			ThrowNull(replacement, nameof(replacement));
			if (replacement.FromPage != null && (replacement.FromPage.IsRedirect || replacement.FromPage.IsDisambiguation))
			{
				replacement.Actions |= ReplacementActions.Edit;
				replacement.Reason = "Dragonborn page is redirect or disambig";
			}
			else if (replacement.ToPage != null && (replacement.ToPage.IsRedirect || replacement.ToPage.IsDisambiguation))
			{
				replacement.Actions |= ReplacementActions.Edit;
				replacement.Reason = "Skyrim page is redirect or disambig";
			}
			else
			{
				replacement.Actions |= ReplacementActions.Move;
				replacement.Reason = "Merge";
			}
		}

		protected override void MovePages()
		{
			var fromPages = new PageCollection(this.Site);
			var toPages = new PageCollection(this.Site);
			foreach (var replacement in this.Replacements)
			{
				if (!replacement.Actions.HasFlag(ReplacementActions.Move))
				{
					continue;
				}

				if (replacement.FromPage == null || replacement.ToPage == null)
				{
					throw new InvalidOperationException();
				}

				if (!replacement.FromPage.IsRedirect)
				{
					var text = replacement.ToPage.Text + "\n\n= Dragonborn {{DB}} =\n" + replacement.FromPage.Text;
					var toPageParser = ContextualParser.FromText(replacement.To, text, null);
					toPageParser.AddCategory("DBMerge-Merged");

					this.ReplaceNodes(replacement.ToPage, toPageParser);

					replacement.ToPage.Text = WikiTextVisitor.Raw(toPageParser);
				}

				replacement.FromPage.Text = $"#REDIRECT [[{replacement.To}]]\n\n[[Category:Redirects from Moves]]";

				if (replacement.FromPage.TextModified)
				{
					fromPages.Add(replacement.FromPage);
				}

				if (replacement.ToPage.TextModified)
				{
					toPages.Add(replacement.ToPage);
				}
			}

			fromPages.RemoveUnchanged();
			toPages.RemoveUnchanged();
			toPages.Sort();
			fromPages.Sort();

			this.ProgressMaximum = fromPages.Count + toPages.Count;
			foreach (var page in toPages)
			{
				this.SavePage(page, "Merge Dragonborn information", false);
				this.Progress++;
			}

			foreach (var page in fromPages)
			{
				this.SavePage(page, "Change to redirect - information merged into Skyrim space", false);
				this.Progress++;
			}
		}

		protected override void PopulateReplacements()
		{
			var dbCheckPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties);
			dbCheckPages.GetNamespace(UespNamespaces.Dragonborn, Filter.Any);

			var ignored = new TitleCollection(this.Site);
			ignored.GetCategoryMembers("DBMerge-No Bot");
			foreach (var title in ignored)
			{
				dbCheckPages.Remove(title);
			}

			var deleted = new TitleCollection(this.Site);
			deleted.GetCategoryMembers("Marked for Deletion");
			foreach (var title in deleted)
			{
				dbCheckPages.Remove(title);
			}

			foreach (var title in DBMergeArticles.SpecialCases.Keys)
			{
				dbCheckPages.Remove(new TitleParts(this.Site, UespNamespaces.Dragonborn, title));
			}

			var srTitles = new TitleCollection(this.Site);
			foreach (var dbCheckPage in dbCheckPages)
			{
				srTitles.Add(new Title(this.Site, UespNamespaces.Skyrim, dbCheckPage.PageName));
			}

			var srPages = new PageCollection(this.Site, PageModules.Info | PageModules.Properties | PageModules.Revisions);
			srPages.GetTitles(srTitles);
			srPages.RemoveNonExistent();
			srPages.Sort();
			foreach (var srPage in srPages)
			{
				var dbTitle = new Title(this.Site, UespNamespaces.Dragonborn, srPage.PageName);
				var srFragmented = new TitleParts(srPage)
				{
					Fragment = "Dragonborn"
				};

				this.Replacements.Add(new Replacement(dbTitle, srFragmented));
			}
		}
		#endregion

		#region Private Methods
		private void AboutHandler(Page page, TemplateNode template)
		{
			foreach (var parameter in template.NumberedParameters)
			{
				var index = parameter.Index;
				if (index > 1 && (index & 1) == 1)
				{
					var value = parameter.Parameter.Value;
					var split = EmbeddedValue.FindWhitespace(WikiTextVisitor.Value(value));
					var title = new Title(this.Site, page.NamespaceId, split.Value);
					if (this.Replacements.TryGetValue(title, out var replacement) && replacement.Actions.HasFlag(ReplacementActions.Move))
					{
						// Note: for simplicity, this pays no attention to ns_base, which is not in use anywhere on the wiki as of this writing.
						split.Value = replacement.To.NamespaceId == page.NamespaceId ? replacement.To.PageName : replacement.To.FullPageName;
						value.Clear();
						value.AddText(split.ToString());
					}
				}
			}
		}

		private void AddRedirCat(Page page, Replacement replacement)
		{
			if (page.NamespaceId != UespNamespaces.Dragonborn)
			{
				throw new InvalidOperationException();
			}

			if (page.Text.Contains("#Dragonborn", StringComparison.Ordinal))
			{
				return;
			}

			var parser = ContextualParser.FromPage(page);
			if (parser.AddCategory("DBMerge-Redirects"))
			{
				page.Text = WikiTextVisitor.Raw(parser);
			}
		}

		private void DbHandler(Page page, TemplateNode template)
		{
			if (template.Parameters.Count > 0)
			{
				var parameter = template.FindNumberedParameter(1);
				if (parameter is ParameterNode foundNode)
				{
					DBMergeArticles.UpdateFileParameter(this, UespNamespaces.Dragonborn, foundNode.Value);
					template.Title.Clear();
					template.Title.AddText("SR");
				}
			}
		}
		#endregion
	}
}
