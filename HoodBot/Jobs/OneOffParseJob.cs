namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.Robby.Design;
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
		protected override string EditSummary => "Remove redundant savetitlename";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Online Achievement Summary");

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			if (parsedPage.FindTemplate("Online Collectible Summary") is SiteTemplateNode template &&
				template.Find("titlename") is IParameterNode title &&
				string.Equals(title.Value.ToValue().Trim(), parsedPage.Context.LabelName(), StringComparison.Ordinal))
			{
				template.Remove("titlename");
			}
		}
		#endregion
	}
}