namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using System.Threading;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.TaskResults;
	using RobinHood70.Robby;
	using RobinHood70.WikiClasses;
	using RobinHood70.WikiCommon;
	using static RobinHood70.WikiCommon.Globals;

	public class GetTemplateUsage : WikiJob
	{
		#region Fields
		private readonly string saveLocation;
		private readonly IReadOnlyList<string> originalTemplateNames;
		private readonly bool respectRedirects;
		#endregion

		#region Constructors
		[JobInfo("Template Usage")]
		public GetTemplateUsage(
			Site site,
			AsyncInfo asyncInfo,
			IEnumerable<string> templateNames,
			[JobParameter(DefaultValue = true)] bool respectRedirects,
			[JobParameterFile(Overwrite = true, DefaultValue = @"%BotData%\%templateName%.txt")] string location)
			: base(site, asyncInfo)
		{
			ThrowNull(templateNames, nameof(templateNames));
			ThrowNull(location, nameof(location));
			this.respectRedirects = respectRedirects;
			var allTemplateNames = new List<string>();
			foreach (var templateName in templateNames)
			{
				allTemplateNames.AddRange(templateName.Split('|'));
			}

			this.saveLocation = location.Replace("%templateName%", allTemplateNames[0]);
			this.originalTemplateNames = allTemplateNames;
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var templates = new TitleCollection(this.Site.Namespaces[MediaWikiNamespaces.Template], this.originalTemplateNames);
			TitleCollection allTemplateNames;
			if (this.respectRedirects)
			{
				this.StatusWriteLine("Loading template redirects");
				templates = TitleCollection.CopyFrom(this.FollowRedirects(templates));
				allTemplateNames = BuildRedirectList(templates);
				this.ProgressMaximum++;
				this.Progress++;
				Thread.Yield();
				Thread.Sleep(1000);
			}
			else
			{
				allTemplateNames = templates;
			}

			this.StatusWriteLine("Loading pages");
			var results = PageCollection.Unlimited(this.Site);
			results.GetPageTranscludedIn(templates);
			this.Progress++;
			Thread.Yield();
			Thread.Sleep(1000);
			this.StatusWriteLine("Exporting");
			this.ExportResults(allTemplateNames, results, this.saveLocation);
			this.Progress++;
			Thread.Yield();
			Thread.Sleep(1000);
		}
		#endregion

		#region Private Methods
		private void ExportResults(TitleCollection allTemplateNames, PageCollection results, string location)
		{
			var allNames = new List<string>(allTemplateNames.ToStringEnumerable(MediaWikiNamespaces.Template));
			var allTemplates = TemplateCollection.GetTemplates(allNames, results);
			if (allTemplates.Count == 0)
			{
				this.StatusWriteLine("No template calls found!");
			}
			else if (!string.IsNullOrWhiteSpace(this.saveLocation))
			{
				try
				{
					this.WriteFile(allTemplates);
					this.StatusWriteLine("File saved to " + location);
				}
				catch (System.IO.IOException e)
				{
					this.StatusWriteLine("Couldn't save file to " + location);
					this.StatusWriteLine(e.Message);
				}
			}
		}

		private void WriteFile(TemplateCollection allTemplates)
		{
			var csvFile = new CsvFile()
			{
				EmptyFieldText = " "
			};
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
					// For now, we're assuming that trimming trailing lines from anon parameters is desirable, but could be made optional if needed.
					row[param.Name] = param.Anonymous ? param.Value.TrimEnd(new[] { '\r', '\n' }) : param.Value;
				}
			}

			csvFile.WriteFile(this.saveLocation);
		}
		#endregion
	}
}