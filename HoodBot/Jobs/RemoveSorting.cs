namespace RobinHood70.HoodBot.Jobs
{
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon;

	internal sealed class RemoveSorting : EditJob
	{
		#region Fields
		private readonly TitleCollection categories;
		#endregion

		#region Constructors
		[JobInfo("Remove Forced Sorting")]
		public RemoveSorting(JobManager jobManager)
			: base(jobManager)
		{
			this.categories = new TitleCollection(this.Site)
			{
				"Category:Morrowind-NPC Images",
				"Category:Bloodmoon-NPC Images",
				"Category:Tribunal-NPC Images",
				"Category:Oblivion-NPC Images",
				"Category:Shivering-NPC Images"
			};
		}
		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Remove forced sorting";

		protected override void LoadPages()
		{
			foreach (var category in this.categories)
			{
				this.Pages.GetCategoryMembers(category.FullPageName(), CategoryMemberTypes.File, false);
			}
		}

		protected override void PageLoaded(Page page)
		{
			var parser = new ContextualParser(page);
			foreach (var link in parser.LinkNodes)
			{
				var linkTitle = TitleFactory.FromUnvalidated(this.Site, link.Title.ToValue());
				if (this.categories.Contains(linkTitle.FullPageName()))
				{
					link.Parameters.Clear();
				}
			}

			parser.UpdatePage();
		}
		#endregion
	}
}