namespace RobinHood70.HoodBot.Jobs
{
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using static RobinHood70.CommonCode.Globals;

	public class OneOffJob : ParsedPageJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob([NotNull, ValidatedNotNull] Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
		}

		protected override string EditSummary => "Remove end parameter";

		protected override void LoadPages() => this.Pages.GetBacklinks("Template:ESO Antiquity", WikiCommon.BacklinksTypes.EmbeddedIn);

		protected override void ParseText(object sender, ContextualParser parsedPage)
		{
			ThrowNull(parsedPage, nameof(parsedPage));
			foreach (var template in parsedPage.FindTemplates("ESO Antiquity"))
			{
				template.Remove("end");
			}
		}
		#endregion
	}
}