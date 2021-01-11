namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Uesp;
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
		protected override string EditSummary => "Remove redundant parameter";
		#endregion

		#region Protected Override Methods

		protected override void LoadPages() => this.Pages.GetNamespace(UespNamespaces.OblivionMod, CommonCode.Filter.Exclude, "Oscuro");

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			foreach (var template in parsedPage.TemplateNodes)
			{
				if (template.TitleValue.PageNameEquals("Trail")
					&& template.Find(1) is IParameterNode param
					&& param.Value.ToValue().Equals("OOO", StringComparison.OrdinalIgnoreCase))
				{
					template.Parameters.Remove(param);
				}
			}
		}
		#endregion
	}
}