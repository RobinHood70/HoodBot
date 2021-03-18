namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	public class MovePages : MovePagesJob
	{
		#region Constructors
		[JobInfo("Page Mover")]
		public MovePages(JobManager jobManager)
			: base(jobManager)
		{
			this.DeleteStatusFile();
			this.MoveAction = MoveAction.MoveSafely;
			this.MoveDelay = 250;
		}
		#endregion

		#region Protected Override Methods
		protected override void PopulateReplacements()
		{
			// => this.LoadReplacementsFromFile(UespSite.GetBotDataFolder("images_to_be_moved.txt"));
			// this.AddReplacement("Category:Beyond Skyrim: Cyrodiil-Interior Images", "Category:Beyond Skyrim-Cyrodiil-Interior Images");
			this.AddFileSpace("BC", "BC4");
			this.AddFileSpace("BAN", "BC4");
			this.AddFileSpace("T3", "MWMOD");
			this.AddFileSpace("T4", "OBMOD");
			this.AddFileSpace("T5", "SRMOD");
			this.AddFileSpace("TOther", "MOD");
		}

		protected override void UpdateLinkNode(Page page, SiteLinkNode node, bool isRedirectTarget)
		{
			var link = SiteLink.FromLinkNode(this.Site, node);
			if (link.Text != null && !link.Coerced)
			{
				var textTitle = Title.FromName(this.Site, link.Text);
				if (node is SiteLinkNode siteLinkNode && textTitle == siteLinkNode.TitleValue)
				{
					link.Text = null;
					link.UpdateLinkNode(node);
				}
			}

			base.UpdateLinkNode(page, node, isRedirectTarget);
		}
		#endregion

		#region Private Methods
		private void AddFileSpace(string from, string to)
		{
			var titles = new TitleCollection(this.Site);
			titles.GetNamespace(MediaWikiNamespaces.File, Filter.Any, from + "-");
			foreach (var title in titles)
			{
				var toName = title.FullPageName.Replace(
					$":{from}-",
					$":{to}-",
					StringComparison.Ordinal);
				this.AddReplacement(title, Title.FromName(this.Site, toName));
			}
		}
		#endregion
	}
}