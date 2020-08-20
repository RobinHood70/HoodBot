namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.CommonCode;
	using RobinHood70.HoodBot.Jobs.Design;
	using RobinHood70.Robby;
	using RobinHood70.WikiCommon;
	using static RobinHood70.CommonCode.Globals;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public MovePages(Site site, AsyncInfo asyncInfo)
			: base(site, asyncInfo)
		{
			this.MoveAction = MoveAction.None;
			this.FollowUpActions |= FollowUpActions.UpdateCategoryMembers;
			this.DeleteFiles();
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			var titles = new TitleCollection(this.Site);
			titles.GetNamespace(MediaWikiNamespaces.Category, Filter.Any, "Dragonborn");
			foreach (var title in titles)
			{
				var newTitle = new Title(title.Namespace, "Skyrim-" + title.PageName);
				this.AddReplacement(title, newTitle);
			}
		}

		protected override void FilterBacklinks(TitleCollection backlinkTitles)
		{
			ThrowNull(backlinkTitles, nameof(backlinkTitles));
			backlinkTitles.Remove("Project:Community Portal");
			backlinkTitles.Remove("Project:Dragonborn Merge Project");
			backlinkTitles.Remove("Project:Dragonborn Merge Project/Merge Results");
			backlinkTitles.Remove("User:Kiz/Sandbox1");
			base.FilterBacklinks(backlinkTitles);
		}
		#endregion
	}
}