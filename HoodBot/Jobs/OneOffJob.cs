namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffJob : EditJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void BeforeLogging()
		{
			this.Pages.PageLoaded += Pages_PageLoaded;
			this.Pages.GetBacklinks("Template:ESO House Furnishings", BacklinksTypes.EmbeddedIn);
			this.Pages.PageLoaded -= Pages_PageLoaded;
		}

		protected override void Main() => this.SavePages("Add furnished parameter");
		#endregion

		#region Private Static Methods
		private static void Pages_PageLoaded(PageCollection sender, Page page)
		{
			var parser = new ContextualParser(page);
			var sections = parser.ToSections(3);
			foreach (var section in sections)
			{
				if (section.Header is IHeaderNode header && string.Equals(header.GetInnerText(true), "Furnished", StringComparison.Ordinal))
				{
					if (section.Content.Find<SiteTemplateNode>(template => template.TitleValue.PageNameEquals("ESO House Furnishings")) is SiteTemplateNode template)
					{
						template.AddIfNotExists("furnished", "1", ParameterFormat.OnePerLine);
					}
				}
			}

			parser.FromSections(sections);
			parser.UpdatePage();

			if (!parser.Page.TextModified)
			{
				Debug.WriteLine("Furnished section not found on " + parser.Page.FullPageName);
			}
		}
		#endregion
	}
}