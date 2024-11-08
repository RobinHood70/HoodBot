namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Upload Antiquity Leads", "ESO Update")]
	internal sealed class UploadAntiquityLeads(JobManager jobManager) : TemplateJob(jobManager)
	{
		#region Private Constants
		private const string VarName = "originalfile";
		#endregion

		#region Fields
		private readonly Dictionary<string, Page> filePages = [];
		private readonly Dictionary<int, Lead> leads = [];
		#endregion

		#region Protected Override Properties
		protected override string TemplateName => "Online Furnishing Antiquity/Row";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => page.Exists ? "Update antiquity lead info" : "Create redirect";

		protected override void LoadPages()
		{
			this.GetIcons(EsoLog.LatestDBUpdate(false));
			this.GetFilePages();
			var leadFiles = this.GetLeadFiles();
			this.GetLeadsFromDb();
			var redirects = this.GetRedirects(leadFiles);
			this.UpdateFilePages(redirects);
			this.Pages.GetBacklinks("Template:Online Furnishing Antiquity/Row", BacklinksTypes.EmbeddedIn, false, Filter.Exclude);
		}

		protected override void ParseTemplate(SiteTemplateNode template, SiteParser parser)
		{
			string? idText = template.GetValue("id");
			if (idText is not null)
			{
				var id = int.Parse(idText, CultureInfo.CurrentCulture);
				var lead = this.leads[id];
				var iconRemote = lead.Icon;
				if (!this.filePages.TryGetValue(iconRemote, out var icon))
				{
					return;
				}

				var title = icon.Title;
				var defaultTitle = $"ON-icon-lead-{parser.Page.Title.PageName}.png";
				if (!title.PageName.OrdinalEquals(defaultTitle))
				{
					template.UpdateIfEmpty("icon", title.PageName);
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static void UpdateFilePage(Title from, Page to)
		{
			var parser = new SiteParser(to);
			if (EsoSpace.FindOrCreateOnlineFile(parser) is not SiteTemplateNode template)
			{
				// Template should ALWAYS be found, but in the unlikely event of a major change, display warning and continue.
				Debug.WriteLine("Template not found on " + to.Title.FullPageName());
				return;
			}

			// A bit kludgy compared to just finding the correct insert position, but ensures that the values are always sorted in pairs and after any named parameters.
			if (!template.TitleNodes.ToRaw().EndsWith('\n'))
			{
				template.TitleNodes.AddText("\n");
			}

			var trimmed = Lead.TrimCruft(from.PageName);
			EsoSpace.AddToOnlineFile(template, "Lead", trimmed);

			parser.UpdatePage();
		}
		#endregion

		#region Private Methods
		private void GetFilePages()
		{
			this.StatusWriteLine("Getting file pages");
			var uesp = (UespSite)this.Site;
			var pages = uesp.CreateMetaPageCollection(PageModules.Default | PageModules.Custom, true, false, VarName);
			pages.SetLimitations(LimitationType.OnlyAllow, UespNamespaces.File);
			pages.GetCustomGenerator(new VariablesInput() { Variables = [VarName] });
			foreach (var page in pages)
			{
				var varPage = (VariablesPage)page;
				if (varPage.GetVariable(VarName) is string origFile)
				{
					var split = origFile.Split(TextArrays.Comma);
					foreach (var name in split)
					{
						var trimmed = name.Trim();
						this.filePages.TryAdd(trimmed, page);
					}
				}
			}
		}

		private TitleCollection GetLeadFiles()
		{
			this.StatusWriteLine("Adding leads");
			var leadTitles = new TitleCollection(this.Site);
			leadTitles.GetNamespace(MediaWikiNamespaces.File, Filter.Only, Lead.Prefix);
			return leadTitles;
		}

		private void GetLeadsFromDb()
		{
			this.StatusWriteLine("Getting leads from database");
			var query = Database.RunQuery(EsoLog.Connection, "SELECT icon, id, name, setName FROM antiquityLeads", row => new Lead(row));
			foreach (var item in query)
			{
				this.leads.Add(item.Id, item);
			}
		}

		private Dictionary<Title, Page> GetRedirects(TitleCollection leadFiles)
		{
			this.StatusWriteLine("Getting redirects");
			var redirects = new Dictionary<Title, Page>();
			foreach (var lead in this.leads.Values)
			{
				if (this.filePages.TryGetValue(lead.Icon, out var page))
				{
					var leadTitle = TitleFactory.FromUnvalidated(this.Site, lead.FileTitle);
					if (!leadFiles.Contains(leadTitle))
					{
						if (!redirects.TryGetValue(leadTitle, out var newTitle) ||
							string.Compare(page.Title.PageName, newTitle.Title.PageName, StringComparison.Ordinal) == -1)
						{
							redirects[leadTitle] = page;
						}
					}
				}
				else
				{
					var filePage = this.UploadFile(lead);
					this.filePages.Add(lead.Icon, filePage);
				}
			}

			return redirects;
		}

		private void UpdateFilePages(Dictionary<Title, Page> redirects)
		{
			this.StatusWriteLine("Updating file pages");
			foreach (var kvp in redirects)
			{
				if (this.Pages.TryGetValue(kvp.Value, out var page))
				{
					UpdateFilePage(kvp.Key, page);
				}
				else
				{
					UpdateFilePage(kvp.Key, kvp.Value);
					this.Pages.Add(kvp.Value);
				}
			}
		}

		private Page UploadFile(Lead lead)
		{
			var pageText =
				$"{{{{Online File\n" +
				$"|originalfile={lead.Icon}\n" +
				$"|Lead|{lead.Name}\n}}}}\n\n" +
				$"[[Category:Online-Icons-Antiquity Leads]]";
			var page = this.Site.CreatePage(lead.FileTitle, pageText);
			var iconName = lead.Icon
				.Replace("esoui/art/", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace('/', '\\');
			var fileName = LocalConfig.BotDataSubPath(iconName + ".png");
			if (this.Site.Upload(fileName, lead.FileTitle, "Upload antiquity lead", page.Text) == ChangeStatus.Failure)
			{
				this.StatusWriteLine("File not found: " + fileName);
			}

			return page;
		}
		#endregion

		#region Private Classes
		private sealed class Lead(IDataRecord row)
		{
			#region Public Static Properties
			public static string Prefix => "ON-icon-lead-";
			#endregion

			#region Public Properties
			public string FileTitle { get; } = "File:" + Prefix + (string)row["name"] + ".png";

			public string Icon { get; } = ((string)row["icon"])[1..^4];

			public int Id { get; } = (int)row["id"];

			public string Name { get; } = (string)row["name"];

			public string SetName { get; } = (string)row["setName"];
			#endregion

			#region Public Static Functions
			public static string TrimCruft(string title)
			{
				title = title[Prefix.Length..];
				var revIndex = title.LastIndexOf('.');
				return revIndex == -1
					? throw new InvalidOperationException()
					: title[..revIndex];
			}
			#endregion
		}
		#endregion
	}
}