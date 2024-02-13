﻿namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;

	public class OneOffMoveJob : MovePagesJob
	{
		#region Constructors
		[JobInfo("One-Off Move Job")]
		public OneOffMoveJob(JobManager jobManager, bool updateUserSpace)
				: base(jobManager, updateUserSpace)
		{
			this.MoveAction = MoveAction.None;
			this.SuppressRedirects = false;
			this.FollowUpActions =
				FollowUpActions.CheckLinksRemaining |
				FollowUpActions.EmitReport |
				FollowUpActions.FixLinks |
				FollowUpActions.RetainDirectLinkText;
			// this.Site.WaitForJobQueue();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateMoves() => this.AddLinkUpdate("Online:Pets", "Online:Non-Combat Pets");

		protected override SiteLink GetToLink(Page page, bool isRedirectTarget, SiteLink from, Title to)
		{
			if (from.Fragment is not null)
			{
				to = TitleFactory.FromValidated(to.Namespace, from.Fragment);
				from.Fragment = null;
			}

			return base.GetToLink(page, isRedirectTarget, from, to);
		}

		//// this.AddLinkUpdate("Category:Online-Furnishings", "Category:Online-Furnishing Images");
		//// this.AddReplacement("Skyrim:Map Notes", "Skyrim:Treasure Maps");
		//// this.LoadReplacementsFromFile(LocalConfig.BotDataSubPath("Replacements5.txt"));
		#endregion
	}
}