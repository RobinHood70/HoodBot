namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.Robby.Parser;
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

		#region Protected Override Properties
		protected override string EditSummary => "Remove redundant template";
		#endregion

		#region Protected Override Methods

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Lore People Summary", WikiCommon.BacklinksTypes.EmbeddedIn, true, CommonCode.Filter.Any);

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			for (var i = parsedPage.Nodes.Count - 1; i >= 0; i--)
			{
				if (parsedPage.Nodes[i] is SiteTemplateNode template && template.TitleValue.PageNameEquals("Lore People Trail"))
				{
					if (template.Find(1) is IParameterNode param)
					{
						if (parsedPage.FindTemplate("Lore People Summary") is ITemplateNode summary)
						{
							summary.Add("letter", $"{param.Value.ToValue().Substring(0, 1)}\n", false);
						}
						else
						{
							throw new InvalidOperationException();
						}
					}

					parsedPage.Nodes.RemoveAt(i);
					if ((i < parsedPage.Parameters.Count - 1) && parsedPage.Nodes[i + 1] is ITextNode text)
					{
						text.Text = text.Text.TrimStart();
					}
				}
			}
		}
		#endregion
	}
}