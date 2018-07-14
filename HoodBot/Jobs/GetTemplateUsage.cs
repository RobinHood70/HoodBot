namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.IO;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.TaskResults;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;

	public class GetTemplateUsage : TaskJob
	{
		#region Constructors
		[JobInfo("Template Usage")]
		public GetTemplateUsage(
			Site site,
			AsyncInfo asyncInfo,
			IEnumerable<string> templateNames,
			[JobParameter(DefaultValue = true)] bool respectRedirects,
			[JobParameterFile(Overwrite = true)] string location)
			: base(site, asyncInfo)
		{
			var templates = new TitleCollection(this.Site)
			{
				{ MediaWikiNamespaces.Template, templateNames }
			};
			var redirectList = new TitleCollection(this.Site);
			if (respectRedirects)
			{
				this.Tasks.Add(new BuildRedirectList(this, templates, redirectList));
			}
			else
			{
				redirectList.AddCopy(templates);
			}

			var pageList = new PageCollection(this.Site);
			this.Tasks.Add(new GetTemplatePages(this, templates, respectRedirects, pageList));
			this.Tasks.Add(new LocalExportTask(this, redirectList, pageList, location));
		}
		#endregion

		#region Private Classes
		private class LocalExportTask : WikiTask
		{
			private readonly string location;
			private readonly PageCollection pages;
			private readonly TitleCollection redirectList;

			public LocalExportTask(WikiRunner parent, TitleCollection redirectList, PageCollection pages, string location)
				: base(parent)
			{
				this.location = location;
				this.pages = pages;
				this.redirectList = redirectList;
			}

			protected override void Main()
			{
				this.StatusWriteLine("Exporting");
				this.ProgressMaximum = 2;

				var allNames = new List<string>(this.redirectList.ToStringEnumerable(MediaWikiNamespaces.Template));
				var allTemplates = TemplateCollection.GetTemplates(allNames, this.pages);
				this.IncrementProgress();

				if (!string.IsNullOrWhiteSpace(this.location))
				{
					this.WriteFile(allTemplates);
				}

				this.IncrementProgress();
			}

			private void WriteFile(TemplateCollection allTemplates)
			{
				using (var file = new StreamWriter(this.location))
				{
					var output = new string[allTemplates.HeaderOrder.Count + 2];
					output[0] = "Page";
					output[1] = "Template Name";
					foreach (var header in allTemplates.HeaderOrder)
					{
						output[header.Value + 2] = header.Key;
					}

					file.WriteLine(string.Join("\t", output));

					foreach (var row in allTemplates)
					{
						output = new string[allTemplates.HeaderOrder.Count + 2];
						output[0] = row.Page;
						output[1] = row.Template.Name;
						foreach (var param in row.Template)
						{
							output[allTemplates.HeaderOrder[param.Name] + 2] = param.Value;
						}

						file.WriteLine(string.Join("\t", output));
					}
				}
			}
		}
		#endregion
		/*
	#region Protected Override Methods
	protected override void Task()
	{
		var searchList = new List<string>(this.templateNames.Count);
		foreach (var templateName in this.templateNames)
		{
			searchList.AddRange(this.TemplateAndRedirects(templateName));
		}

		var searchListFinder = Template.Find(searchList);
		this.FillPageList();
		if (this.PageList.pages.Count == 0)
		{
			this.UpdateStatus("There were no transclusions.");
			return;
		}

		this.PageList.Sort();
		this.Execute(this.PageList.LoadWithMetadata);
		this.InitializeProgress(this.PageList.pages.Count);

		if (!Directory.Exists(BotPath))
		{
			Directory.CreateDirectory(BotPath);
		}

		var fileName = this.Site.RemoveNsPrefix(this.templateNames[0], Uesp.Template).Replace('/', '_').Replace(':', '_');
		using (var f = File.CreateText(Path.Combine(BotPath, "Links to " + fileName + ".txt")))
		{
			foreach (var page in this.PageList.pages)
			{
				try
				{
					var matches = searchListFinder.Matches(page.text);
					if (matches.Count > 0)
					{
						f.WriteLine(page.title);
						foreach (Match match in matches)
						{
							f.WriteLine("  " + match.ToString().Replace("\n", "\n  "));
						}

						f.WriteLine();
						f.WriteLine();
					}
				}
				catch (RegexMatchTimeoutException)
				{
					this.SendWarning("Regex timed out on {0}", page.title);
				}
				catch (StopException)
				{
					break;
				}

				this.Progress++;
			}
		}
	}
	#endregion

	#region Private Methods
	private IEnumerable<string> TemplateAndRedirects(string templateName)
	{
		var retval = new List<string>();
		var templateShort = templateName;
		templateShort = this.Site.RemoveNsPrefix(templateShort, Uesp.Template);
		retval.Add(templateShort);

		this.UpdateStatus("Getting redirects to " + templateShort);
		var redirs = new PageList(this.Site);
		redirs.FillFromLinksToPage(templateName, LinkTypes.Redirects);
		foreach (var redir in redirs.pages)
		{
			var title = this.Site.RemoveNsPrefix(redir.title, Uesp.Template);
			retval.Add(title);
		}

		return retval;
	}

	private void FillPageList()
	{
		this.UpdateStatus("Getting template usage");
		foreach (var templateName in this.templateNames)
		{
			this.PageList.FillFromLinksToPage(templateName, LinkTypes.Transclusions);
		}
	}
	#endregion

		 */
	}
}