namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	public class OneOffJob : EditJob
	{
		#region Constructors
		[JobInfo("One-Off Job")]
		public OneOffJob(JobManager jobManager)
			: base(jobManager)
		{
		}
		#endregion

		#region Protected Override Properties
		protected override Action<EditJob, Page>? EditConflictAction => Pages_PageLoaded;

		protected override string EditSummary => "Add furnished parameter";
		#endregion

		#region Protected Override Methods
		protected override void LoadPages() =>
			this.Pages.GetNamespace(UespNamespaces.Online, CommonCode.Filter.Exclude, "Guild Reprint");
		#endregion

		#region Private Static Methods
		private static void Pages_PageLoaded(object sender, Page page)
		{
			var parser = new ContextualParser(page);
			var template = parser.FindSiteTemplate("Online Furnishing Summary");
			if (template is not null && template.Find("cat", "subcat") is null)
			{
				template.Parameters.Insert(0, parser.Factory.ParameterNodeFromParts("cat", "Library\n"));
				template.Parameters.Insert(1, parser.Factory.ParameterNodeFromParts("subcat", "Literature\n"));
			}

			parser.UpdatePage();
		}
		#endregion
	}
}