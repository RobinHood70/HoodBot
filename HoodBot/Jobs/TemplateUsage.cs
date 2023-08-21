namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using RobinHood70.CommonCode;

	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	internal class TemplateUsage : WikiJob
	{
		#region Fields
		private readonly string saveLocation;
		private readonly bool respectRedirects;
		private readonly List<string> headerOrder = new();
		private readonly bool checkAllTemplates;
		private readonly TitleCollection allTemplateNames;
		#endregion

		#region Constructors
		[JobInfo("Template Usage")]
		public TemplateUsage(
			JobManager jobManager,
			IEnumerable<string> templateNames,
			[JobParameter(DefaultValue = true)] bool respectRedirects,
			[JobParameterFile(Overwrite = true, DefaultValue = @"%BotData%\%templateName%.txt")] string location,
			bool checkAllTemplates)
			: base(jobManager, JobType.ReadOnly)
		{
			location.ThrowNull();
			this.respectRedirects = respectRedirects;
			this.checkAllTemplates = checkAllTemplates;
			List<string> allNames = new();
			foreach (var templateName in templateNames.NotNull())
			{
				allNames.AddRange(templateName.Split(TextArrays.NewLineChars, StringSplitOptions.RemoveEmptyEntries));
			}

			this.saveLocation = location.Replace("%templateName%", Globals.SanitizeFilename(allNames[0]), StringComparison.Ordinal);
			this.allTemplateNames = new(this.Site, MediaWikiNamespaces.Template, allNames);
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			// CONSIDER: Adapt this and/or the parser to handle relative templates like {{/Template}} and {{../Template}}.
			if (this.respectRedirects)
			{
				this.StatusWriteLine("Loading template redirects");
				this.BuildRedirectList();
			}

			this.StatusWriteLine("Loading pages");
			var results = PageCollection.Unlimited(this.Site);
			if (this.checkAllTemplates)
			{
				results.GetNamespace(MediaWikiNamespaces.Template);
			}

			results.GetPageTranscludedIn(this.allTemplateNames);
			this.StatusWriteLine("Exporting");
			results.Sort();
			this.ExportTemplates(results);
		}
		#endregion

		#region Protected Virtual Methods
		protected virtual bool ShouldAddPage(ContextualParser parser) => true;

		protected virtual bool ShouldAddTemplate(SiteTemplateNode template, ContextualParser parser) => true;
		#endregion

		#region Private Static Methods
		private void BuildRedirectList()
		{
			// First, make sure we're at the redirect target of all redirects.
			var pages = this.allTemplateNames.Load(PageModules.None, true);

			pages.RemoveExists(false);
			var titles = pages.ToFullPageNames();

			foreach (var title in titles)
			{
				this.allTemplateNames.TryAdd(title); // Make sure the title iteself is there, in case we started at a redirect.
				this.allTemplateNames.GetBacklinks(title, BacklinksTypes.Backlinks, true, Filter.Only); // Grab immediate redirects of the page.
			}
		}
		#endregion

		#region Private Methods
		private void AddPage(List<(Title Page, ITemplateNode Template)> templates, Dictionary<string, string> paramTranslator, ContextualParser parser)
		{
			foreach (var template in parser.FindAll<SiteTemplateNode>())
			{
				if (this.ShouldAddTemplate(template, parser) && this.allTemplateNames.Contains(template.TitleValue))
				{
					this.AddTemplate(templates, paramTranslator, parser, template);
				}
			}
		}

		private void AddTemplate(List<(Title Page, ITemplateNode Template)> templates, Dictionary<string, string> paramTranslator, ContextualParser parser, SiteTemplateNode template)
		{
			templates.Add((parser.Page.Title, template));
			foreach (var (name, _) in template.GetResolvedParameters())
			{
				if (paramTranslator.TryAdd(name, name))
				{
					this.headerOrder.Add(name);
				}
			}
		}

		private void ExportTemplates(PageCollection pages)
		{
			var templates = this.ExtractTemplates(pages);
			if (templates.Count == 0)
			{
				this.StatusWriteLine("No template calls found!");
				return;
			}

			try
			{
				this.WriteFile(templates, this.saveLocation);
				this.StatusWriteLine("File saved to " + this.saveLocation);
			}
			catch (IOException e)
			{
				this.StatusWriteLine("Couldn't save file to " + this.saveLocation);
				this.StatusWriteLine(e.Message);
			}
		}

		private List<(Title Page, ITemplateNode Template)> ExtractTemplates(PageCollection pages)
		{
			List<(Title Page, ITemplateNode Template)> templates = new();
			Dictionary<string, string> paramTranslator = new(StringComparer.Ordinal); // TODO: Empty dictionary for now, but could be pre-populated to translate synonyms to a consistent name. Similarly, name comparison can be case-sensitive or not. Need to find a useful way to do those.
			foreach (var page in pages)
			{
				ContextualParser parser = new(page);
				if (this.ShouldAddPage(parser))
				{
					this.AddPage(templates, paramTranslator, parser);
				}
			}

			return templates;
		}

		private void WriteFile(List<(Title Page, ITemplateNode Template)> results, string location)
		{
			CsvFile csvFile = new() { EmptyFieldText = " " };
			List<string> output = new(this.headerOrder.Count + 2)
			{
				"Page",
				"Template Name"
			};
			output.AddRange(this.headerOrder);
			csvFile.Header = output;

			foreach (var template in results)
			{
				var row = csvFile.Add(template.Page.FullPageName(), template.Template.GetTitleText());
				foreach (var (name, parameter) in template.Template.GetResolvedParameters())
				{
					// For now, we're assuming that trimming trailing lines from anon parameters is desirable, but could be made optional if needed.
					var value = parameter.Value.ToRaw();
					row[name] = parameter.Anonymous ? value.TrimEnd(TextArrays.NewLineChars) : value.Trim();
				}
			}

			csvFile.WriteFile(location);
		}
		#endregion
	}
}