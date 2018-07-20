namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.TaskResults;
	using RobinHood70.HoodBot.Jobs.Tasks;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses;
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

				if (allTemplates.Count == 0)
				{
					this.StatusWriteLine("No template calls found!");
				}
				else if (!string.IsNullOrWhiteSpace(this.location))
				{
					try
					{
						this.WriteFile(allTemplates);
					}
					catch (System.IO.IOException e)
					{
						this.StatusWriteLine("Couldn't save file!");
						this.StatusWriteLine(e.Message);
					}
				}

				this.IncrementProgress();
			}

			private void WriteFile(TemplateCollection allTemplates)
			{
				var csvFile = new CsvFile();
				var output = new List<string>(allTemplates.HeaderOrder.Count + 2)
				{
					"Page",
					"Template Name"
				};
				output.AddRange(allTemplates.HeaderOrder.Keys);
				csvFile.AddHeader(output);

				foreach (var template in allTemplates)
				{
					var row = csvFile.Add(template.Page, template.Template.Name);
					foreach (var param in template.Template)
					{
						row[param.Name] = param.Value;
					}
				}

				csvFile.WriteFile(this.location);
			}
		}
		#endregion
	}
}