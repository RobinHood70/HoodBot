namespace RobinHood70.HoodBot.Jobs
{
	using System.Text;
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
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
		protected override string EditSummary => "Add explicit header";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages() => this.Pages.GetBacklinks("Template:Similar Images");

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			var templateIndex = parsedPage.Nodes.FindIndex<SiteTemplateNode>(t => t.TitleValue.PageNameEquals("Similar Images"));
			var header = parsedPage.Nodes.Find<IHeaderNode>(h => string.Equals(h.GetInnerText(true), "Similar Iamges", System.StringComparison.Ordinal));
			if (header == null)
			{
				parsedPage.Nodes.Insert(templateIndex, parsedPage.Factory.TextNode("== Similar Images ==\n"));
			}
		}
		#endregion
	}
}