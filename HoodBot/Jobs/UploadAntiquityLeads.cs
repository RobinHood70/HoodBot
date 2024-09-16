﻿namespace RobinHood70.HoodBot.Jobs
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
			var patchVersion = new EsoVersion(43, true);
			this.GetIcons(patchVersion.Text, false);
			this.GetFilePages();

			var leadFiles = new TitleCollection(this.Site);
			leadFiles.GetNamespace(MediaWikiNamespaces.File, Filter.Only, Lead.Prefix);

			var query = Database.RunQuery(EsoLog.Connection, "SELECT icon, id, name, setName FROM antiquityLeads", row => new Lead(row));
			foreach (var item in query)
			{
				this.leads.Add(item.Id, item);
			}

			var redirects = new Dictionary<Title, Page>();
			foreach (var (key, lead) in this.leads)
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

			this.Pages.GetBacklinks("Template:Online Furnishing Antiquity/Row", BacklinksTypes.EmbeddedIn, false, Filter.Exclude);
		}

		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
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
				if (!string.Equals(title.PageName, defaultTitle, StringComparison.Ordinal))
				{
					template.Update("icon", title.PageName);
				}
			}
		}
		#endregion

		#region Private Static Methods
		private static int PairedComparer((string Key, string Value) x, (string Key, string Value) y)
		{
			var compare = string.Compare(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
			return compare == 0
				? string.Compare(x.Value, y.Value, StringComparison.OrdinalIgnoreCase)
				: compare;
		}

		private static void UpdateFilePage(Title from, Page to)
		{
			var parser = new ContextualParser(to);
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
			var anons = new List<(string Key, string Value)>();
			while (i < template.Parameters.Count)
			{
				var param = template.Parameters[i];
				if (param.Anonymous)
				{
					if (i < (template.Parameters.Count - 1))
					{
						var key = param.Value.ToRaw();
						var value = template.Parameters[i + 1].Value.ToRaw().Trim();
						anons.Add((key, value));
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

			var trimmed = Lead.TrimCruft(from.PageName);
			if (!anons.Contains(("Lead", trimmed)))
			{
				anons.Add(("Lead", trimmed));
			}

			anons.Sort(PairedComparer);

			for (i = template.Parameters.Count - 1; i >= 0; i--)
			{
				if (template.Parameters[i].Anonymous)
				{
					template.Parameters.RemoveAt(i);
				}
			}

			foreach (var (key, value) in anons)
			{
				template.Add(key, ParameterFormat.Packed);
				template.Add(value, ParameterFormat.OnePerLine);
			}

			if (single is not null)
			{
				template.Parameters.Add(single);
			}

			parser.UpdatePage();
		}
		#endregion

		#region Private Methods
		private void GetFilePages()
		{
			var uesp = (UespSite)this.Site;
			var pages = uesp.CreateMetaPageCollection(PageModules.Default | PageModules.Custom, true, false, "originalfile");
			pages.SetLimitations(LimitationType.OnlyAllow, UespNamespaces.File);
			pages.GetCustomGenerator(new VariablesInput() { Variables = ["originalfile"] });
			foreach (var page in pages)
			{
				var varPage = (VariablesPage)page;
				if (varPage.GetVariable("originalfile") is string origFile)
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