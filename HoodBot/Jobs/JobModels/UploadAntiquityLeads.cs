namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Diagnostics;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Design;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("Upload Antiquity Leads")]
	internal class UploadAntiquityLeads(JobManager jobManager) : EditJob(jobManager)
	{
		#region Fields
		private readonly Dictionary<string, Page> originalFileNames = [];
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => page.Exists ? "Update antiquity lead info" : "Create redirect";

		protected override void LoadPages()
		{
			this.Shuffle = true;
			var uesp = (UespSite)this.Site;
			var filePages = uesp.CreateMetaPageCollection(PageModules.Default | PageModules.Custom, true, false, "originalfile");
			filePages.SetLimitations(LimitationType.OnlyAllow, UespNamespaces.File);
			filePages.GetCustomGenerator(new VariablesInput() { Variables = ["originalfile"] });
			foreach (var page in filePages)
			{
				var varPage = (VariablesPage)page;
				if (varPage.GetVariable("originalfile") is string origFile)
				{
					var split = origFile.Split(TextArrays.Comma);
					foreach (var name in split)
					{
						var trimmed = name.Trim();
						this.originalFileNames.TryAdd(trimmed, page);
					}
				}
			}

			var leadFiles = new TitleCollection(this.Site);
			leadFiles.GetNamespace(MediaWikiNamespaces.File, Filter.Only, Lead.Prefix);

			var leads = Database.RunQuery(EsoLog.Connection, "SELECT name, icon, setName FROM antiquityLeads", row => new Lead(row));
			var redirects = new Dictionary<Title, Page>();
			foreach (var lead in leads)
			{
				if (this.originalFileNames.TryGetValue(lead.Icon, out var page))
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
					this.UploadFile(lead);
				}
			}

			foreach (var kvp in redirects)
			{
				if (this.Pages.TryGetValue(kvp.Value, out var page))
				{
					this.CreateRedirect(kvp.Key, page);
				}
				else
				{
					this.CreateRedirect(kvp.Key, kvp.Value);
					this.Pages.Add(kvp.Value);
				}
			}
		}

		protected override void PageLoaded(Page page) => throw new NotSupportedException();
		#endregion

		#region Private Methods
		private void CreateRedirect(Title from, Page to)
		{
			var page = this.Site.CreatePage(from);
			page.Text = "#REDIRECT [[" + to.Title.FullPageName() + "]][[Category:Redirects to Alternate Names]][[Category:Online-Icons-Antiquity Leads]]";
			this.Pages.Add(page);

			var parser = new ContextualParser(to);
			var anons = new List<(IParameterNode Key, IParameterNode Value)>();
			if (parser.FindSiteTemplate("Online File") is not SiteTemplateNode template)
			{
				Debug.WriteLine("Template not found on " + to.Title.FullPageName());
				return;
			}

			// A bit kludgy compared to just finding the correct insert position, but ensures that the values are always sorted in pairs and after any named parameters.
			if (!template.Title.ToRaw().EndsWith('\n'))
			{
				template.Title.AddText("\n");
			}

			var i = 0;
			IParameterNode? single = null;
			while (i < template.Parameters.Count)
			{
				var param = template.Parameters[i];
				if (param.Anonymous)
				{
					if (i < (template.Parameters.Count - 1))
					{
						anons.Add((param, template.Parameters[i + 1]));
					}
					else
					{
						single = param;
					}

					i += 2;
				}
				else
				{
					if (!param.Value.ToRaw().EndsWith('\n'))
					{
						param.Value.AddText("\n");
					}

					i++;
				}
			}

			anons.Add((
				parser.Factory.ParameterNodeFromParts("Lead"),
				parser.Factory.ParameterNodeFromParts(Lead.ReverseTitle(from.PageName) + '\n')));
			anons.Sort(this.PairedComparer);

			for (i = template.Parameters.Count - 1; i >= 0; i--)
			{
				if (template.Parameters[i].Anonymous)
				{
					template.Parameters.RemoveAt(i);
				}
			}

			foreach (var (key, value) in anons)
			{
				template.Parameters.Add(key);
				template.Parameters.Add(value);
			}

			if (single is not null)
			{
				template.Parameters.Add(single);
			}

			parser.UpdatePage();
		}

		private int PairedComparer((IParameterNode Key, IParameterNode Value) x, (IParameterNode Key, IParameterNode Value) y)
		{
			var compare = string.Compare(x.Key.Value.ToRaw(), y.Key.Value.ToRaw(), StringComparison.OrdinalIgnoreCase);
			return compare == 0
				? string.Compare(x.Value.Value.ToRaw(), y.Value.Value.ToRaw(), StringComparison.OrdinalIgnoreCase)
				: compare;
		}

		private void UploadFile(Lead lead)
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

			this.originalFileNames.Add(lead.Icon, page);
		}
		#endregion

		#region Private Classes
		private class Lead(IDataRecord row)
		{
			#region Public Static Properties
			public static string Prefix => "ON-icon-lead-";
			#endregion

			#region Public Properties
			public string FileTitle { get; } = "File:" + Prefix + (string)row["name"] + ".png";

			public string Icon { get; } = ((string)row["icon"])[1..^4];

			public string Name { get; } = (string)row["name"];

			public string SetName { get; } = (string)row["setName"];
			#endregion

			#region Public Static Functions
			public static string ReverseTitle(string title) => title[Prefix.Length..^4];
			#endregion
		}
		#endregion
	}
}
