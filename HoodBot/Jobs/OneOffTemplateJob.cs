namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffTemplateJob : TemplateJob
	{
		#region Fields
		private static readonly Dictionary<string, string> RomanToNum = new(System.StringComparer.Ordinal)
		{
			["I"] = "1",
			["II"] = "2",
			["III"] = "3",
			["IV"] = "4",
			["V"] = "5",
			["VI"] = "6",
			["VII"] = "7",
			["VIII"] = "8",
			["IX"] = "9",
			["X"] = "10",
		};
		#endregion

		#region Constructors
		[JobInfo("One-Off Template Job")]
		public OneOffTemplateJob(JobManager jobManager)
				: base(jobManager)
		{
		}
		#endregion

		#region Public Override Properties
		public override string LogDetails => "Update " + this.TemplateName;

		public override string LogName => "One-Off Template Job";
		#endregion

		#region Protected Override Properties
		protected override string TemplateName => "Planet Infobox";
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Add planet order";

		protected override void LoadPages() => this.Pages.GetCategoryMembers("Category:Starfield-Planets");

		protected override void ParseTemplate(SiteTemplateNode template, ContextualParser parser)
		{
			var lastWord = parser.Page.Title.PageName.Split(' ')[^1];
			if (RomanToNum.TryGetValue(lastWord, out var orderNum))
			{
				template.AddIfNotExists("order", orderNum, ParameterFormat.OnePerLine);
			}
		}
		#endregion
	}
}