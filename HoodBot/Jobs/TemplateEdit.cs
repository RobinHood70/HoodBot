namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;

	public class TemplateEdit : TemplateJob
	{
		[JobInfo("Template Edit")]
		public TemplateEdit(JobManager jobManager)
			: base(jobManager)
		{
		}

		protected override string TemplateName => "Mod Header";

		protected override string EditSummary => "Use CC template";

		protected override void LoadPages() => this.Pages.GetCategoryMembers("Category:Skyrim-Creation Club", true);

		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			template.Title.Clear();
			template.Title.AddText("CC Header");
			if (template.Parameters.Count > 1)
			{
				Debug.WriteLine(parser.Title.FullPageName());
			}
		}
	}
}
