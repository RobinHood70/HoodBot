namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	internal sealed class CreateFactionRedirects : EditJob
	{
		#region Private Constants
		private const string SearchPrefix = "Beyond Skyrim:Cyrodiil/Factions ";
		#endregion

		#region Fields
		private readonly PageCollection allTitles;
		private string? nsFull;
		#endregion

		#region Constructors
		[JobInfo("Create Faction Redirects")]
		public CreateFactionRedirects(JobManager jobManager)
			: base(jobManager)
		{
			this.allTitles = new PageCollection(this.Site, PageModules.Info);
		}
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Create faction redirect";

		protected override void LoadPages()
		{
			var searchTitle = TitleFactory.FromUnvalidated(this.Site, SearchPrefix);
			var fakeNs = new UespNamespaceList(this.Site).FromTitle(searchTitle) ?? throw new InvalidOperationException();
			this.nsFull = fakeNs.Full;

			var baseTitle = TitleFactory.FromUnvalidated(this.Site, fakeNs.Base);
			this.allTitles.GetNamespace(baseTitle.Namespace.Id, Filter.Any, fakeNs.IsPseudoNamespace ? baseTitle.PageName + "/" : string.Empty);

			var factionPages = new PageCollection(this.Site);
			factionPages.GetNamespace(searchTitle.Namespace.Id, Filter.Any, searchTitle.PageName);
			foreach (var factionPage in factionPages)
			{
				var parser = new ContextualParser(factionPage);
				var sections = parser.ToSections(2);
				foreach (var section in sections)
				{
					if (section.Header is null)
					{
						continue;
					}

					var headerTitle = section.Header.GetTitle(true);
					this.CreatePage(factionPage, headerTitle, headerTitle);
					var factionTemplates = section.Content.FindAll<SiteTemplateNode>(template => template.TitleValue.PageNameEquals("Factions"));
					foreach (var node in factionTemplates)
					{
						var edid = node.GetRaw("edid");
						if (edid != null)
						{
							this.CreatePage(factionPage, edid, headerTitle);
						}
					}
				}
			}
		}

		protected override void PageLoaded(Page page)
		{
		}
		#endregion

		#region Private Methods
		private void CreatePage(Page factionPage, string factionName, string section)
		{
			if (factionName.Contains("{{", System.StringComparison.Ordinal))
			{
				this.StatusWriteLine("Ignoring " + factionName);
				return;
			}

			var pageName = this.nsFull + factionName;
			var site = factionPage.Site;
			if (this.allTitles.TryGetValue(pageName, out var existingPage) && !existingPage.IsRedirect)
			{
				pageName += " (faction)";
			}

			if (!this.Pages.Contains(pageName))
			{
				var page = site.CreatePage(pageName);
				page.Text = $"#REDIRECT [[{factionPage.Title.FullPageName()}#{section}]] [[Category:Redirects to Broader Subjects]] [[Category:Beyond Skyrim-Cyrodiil-Factions]]";
				this.Pages.Add(page);
			}
		}
		#endregion
	}
}