namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Jobs.JobModels;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

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
				allTemplateNames.AddRange(templateName.Split(TextArrays.Pipe));
			}

			this.saveLocation = location.Replace("%templateName%", allTemplateNames[0], StringComparison.Ordinal);
			this.originalTemplateNames = allTemplateNames;
			this.ProgressMaximum = 2;
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var templates = new TitleCollection(this.Site, MediaWikiNamespaces.Template, this.originalTemplateNames);
			TitleCollection allTemplateNames;
			if (this.respectRedirects)
			{
				this.StatusWriteLine("Loading template redirects");
				templates = new TitleCollection(this.FollowRedirects(templates));
				allTemplateNames = BuildRedirectList(templates);
				this.ProgressMaximum++;
				this.Progress++;
			}
			else
			{
				allTemplateNames = templates;
			}

			this.StatusWriteLine("Loading pages");
			var results = PageCollection.Unlimited(this.Site);
			results.GetPageTranscludedIn(templates);
			this.Progress++;
			this.StatusWriteLine("Exporting");
			this.ExportResults(allTemplateNames, results, this.saveLocation);
			this.Progress++;
		}
		#endregion

		#region Private Static Methods
		private static TitleCollection BuildRedirectList(IEnumerable<ISimpleTitle> titles)
		{
			var retval = new TitleCollection(titles);

			// Loop until nothing new is added.
			var pagesToCheck = new HashSet<Title>(retval, SimpleTitleEqualityComparer.Instance);
			var alreadyChecked = new HashSet<Title>(SimpleTitleEqualityComparer.Instance);
			do
			{
				foreach (var page in pagesToCheck)
				{
					retval.GetBacklinks(page.FullPageName(), BacklinksTypes.Backlinks, true, Filter.Only);
				}

				alreadyChecked.UnionWith(pagesToCheck);
				pagesToCheck.Clear();
				pagesToCheck.UnionWith(retval);
				pagesToCheck.ExceptWith(alreadyChecked);
			}
			while (pagesToCheck.Count > 0);

			return retval;
		}

		private PageCollection FollowRedirects(TitleCollection titles)
		{
			var originalsFollowed = PageCollection.Unlimited(this.Site, PageModules.None, true);
			originalsFollowed.GetTitles(titles);

			return originalsFollowed;
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
				EmptyFieldText = " ",
			};
			var output = new List<string>(allTemplates.HeaderOrder.Count + 2)
			{
				"Page",
				"Template Name"
			};
			output.AddRange(allTemplates.HeaderOrder.Keys);
			csvFile.Header = output;

			foreach (var template in allTemplates)
			{
				var row = csvFile.Add(template.Page, template.Template.Name);
				foreach (var param in template.Template)
				{
					// For now, we're assuming that trimming trailing lines from anon parameters is desirable, but could be made optional if needed.
					ThrowNull(param.Name, nameof(param), nameof(param.Name));
					row[param.Name] = param.Anonymous ? param.Value.TrimEnd(TextArrays.NewLineChars) : param.Value;
				}
			}

			csvFile.WriteFile(this.saveLocation);
		}
		#endregion
	}
}