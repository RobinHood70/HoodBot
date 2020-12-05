namespace RobinHood70.HoodBot.Jobs
{
	using System.Text.RegularExpressions;
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.HoodBot.Uesp;
	using static RobinHood70.CommonCode.Globals;

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
			var questLinkFixer = new Regex(@"({{Quest Link.*?}}).*?(</noinclude>)?\n", RegexOptions.ExplicitCapture, DefaultRegexTimeout);
			this.Pages.GetNamespace(UespNamespaces.Tes4Mod, Filter.Any, "Better Cities/");
			foreach (var page in this.Pages)
			{
				page.Text = questLinkFixer.Replace(page.Text, "$1$2\n");
			}
		}

		protected override void Main() => this.SavePages("Fix bot error");
		#endregion
	}
}