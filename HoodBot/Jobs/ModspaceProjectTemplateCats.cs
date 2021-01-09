namespace RobinHood70.HoodBot.Jobs
{
	using System;

	public class ModspaceProjectTemplateCats : MovePagesJob
	{
		#region Constructors
		[JobInfo("2-Move NS_NAME categories", "Modspace Project")]
		public ModspaceProjectTemplateCats(JobManager jobManager)
			: base(jobManager, "Template")
		{
			this.DeleteStatusFile();
			this.EditSummaryMove = "Modspace Project: standardize category names";
			this.FollowUpActions = FollowUpActions.EmitReport | FollowUpActions.FixLinks | FollowUpActions.UpdateCategoryMembers;
			this.SuppressRedirects = false;
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			var pageText = this.Site.LoadPageText("User:RobinHood70/Lamed");
			if (string.IsNullOrEmpty(pageText))
			{
				throw new InvalidOperationException();
			}

			var lines = pageText.Split("\n|-\n|", StringSplitOptions.None);
			foreach (var line in lines)
			{
				var split = line.Split("||");
				if (split.Length == 2)
				{
					var from = "Category:" + split[0].Trim();
					var to = "Category:" + split[1].Trim();
					this.AddReplacement(from, to);
				}
			}
		}
		#endregion
	}
}