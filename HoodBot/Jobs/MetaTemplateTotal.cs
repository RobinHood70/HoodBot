namespace RobinHood70.HoodBot.Jobs
{
	using System.Collections.Generic;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using RobinHood70.WallE.Base;
	using RobinHood70.WikiCommon;

	internal sealed class MetaTemplateTotal : WikiJob
	{
		#region Static Fields
		private static readonly string[] MetaTemplateCategories = new[]
		{
			"MetaTemplate-Data",
			"MetaTemplate-Include",
			"MetaTemplate-Listsaved",
			"MetaTemplate-Split Arguments",
			"MetaTemplate-Stack",
			"MetaTemplate-Variables"
		};
		#endregion

		#region Constructors
		[JobInfo("MetaTemplate Totals")]
		public MetaTemplateTotal(JobManager jobManager)
			: base(jobManager, JobType.ReadOnly)
		{
		}
		#endregion

		#region Protected Override Methods
		protected override void Main()
		{
			var input = new SiteInfoInput(SiteInfoProperties.Statistics);
			var results = this.Site.AbstractionLayer.SiteInfo(input);
			var totalPages = results.Statistics?.Pages ?? 0;

			var hashes = new HashSet<Title>(SimpleTitleComparer.Instance);
			this.ProgressMaximum = MetaTemplateCategories.Length;
			foreach (var cat in MetaTemplateCategories)
			{
				var titles = new TitleCollection(this.Site);
				this.StatusWriteLine("Getting " + cat);
				titles.GetCategoryMembers(cat, CategoryMemberTypes.All, false);
				foreach (var title in titles)
				{
					hashes.Add(title);
				}

				this.Progress++;
			}

			var percent = (double)hashes.Count / totalPages;
			this.StatusWriteLine($"{hashes.Count:N0} / {totalPages:N0} ({percent:P}) use MetaTemplate.");
		}
		#endregion
	}
}
