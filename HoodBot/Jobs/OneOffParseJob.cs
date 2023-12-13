namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffParseJob : ParsedPageJob
	{
		#region Constructors
		[JobInfo("One-Off Parse Job")]
		public OneOffParseJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogDetails => "Add System Table";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:System Infobox", BacklinksTypes.EmbeddedIn, true, Filter.Exclude, UespNamespaces.Online);

		protected override void PageLoaded(Page page)
		{
			base.PageLoaded(page);
			page.Text = Regex.Replace(page.Text, @"\s+\n\{\{System Table\}\}", "\n{{System Table}}", RegexOptions.ExplicitCapture, Globals.DefaultRegexTimeout);
			page.Text = page.Text.TrimEnd();
		}

		protected override void ParseText(ContextualParser parser)
		{
			if (parser.FindSiteTemplate("System Table") is not null)
			{
				return;
			}

			var sections = parser.ToSections(2);
			int sectionNum;
			for (sectionNum = 1; sectionNum < sections.Count; sectionNum++)
			{
				var section = sections[sectionNum];
				if (string.Equals(section.Header?.GetTitle(true), "Planets", StringComparison.OrdinalIgnoreCase))
				{
					sections.RemoveAt(sectionNum);
					break;
				}
			}

			sections[sectionNum - 1].Content.AddText("\n{{System Table}}\n\n");
			parser.FromSections(sections);
		}
		#endregion
	}
}