namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Diagnostics;
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
		public override string LogDetails => "Remove Endless Archive header";

		public override string LogName => "One-Off Parse Job";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => this.LogDetails;

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Mod Header", BacklinksTypes.EmbeddedIn, true, Filter.Exclude, UespNamespaces.Online);

		protected override void ParseText(ContextualParser parser)
		{
			for (var templateOffset = parser.Count - 1; templateOffset >= 0; templateOffset--)
			{
				if (parser[templateOffset] is SiteTemplateNode template &&
					template.TitleValue.PageNameEquals("Mod Header") &&
				string.Equals(template.GetValue(1), "Endless Archive", StringComparison.Ordinal))
				{
					// var archive = ((SiteTemplateNode)parser[templateOffset]).GetValue(1);
					Debug.WriteLine(parser.Page.Title.FullPageName());
					parser.RemoveAt(templateOffset);
					if (parser.Count >= templateOffset && parser[templateOffset] is ITextNode textNode)
					{
						textNode.Text = textNode.Text.TrimStart(' ');
						if (textNode.Text.Length > 0 && textNode.Text[0] == '\n')
						{
							if (textNode.Text.Length > 1)
							{
								textNode.Text = textNode.Text[1..];
							}
							else
							{
								parser.RemoveAt(templateOffset);
							}
						}
					}
				}
			}
		}
		#endregion
	}
}